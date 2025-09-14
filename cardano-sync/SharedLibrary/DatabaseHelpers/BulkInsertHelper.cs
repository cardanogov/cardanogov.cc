using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace SharedLibrary.DatabaseHelpers;

public static class BulkInsertHelper
{
    /// <summary>
    /// Performs bulk insert with duplicate detection based on composite key
    /// </summary>
    public static async Task<(int insertedCount, int skippedCount)> BulkInsertWithDuplicateCheck<T>(
        DbContext context,
        ILogger logger,
        List<T> newData,
        string tableName,
        Func<T, string> getCompositeKey,
        Func<T, object[]> getInsertParameters,
        string insertColumnsClause,
        Func<T, string>? getValuePlaceholders = null,
        int batchSize = 5000)
    {
        if (newData.Count == 0)
            return (0, 0);

        logger.LogInformation("📦 Starting bulk insert for {Count} records into {Table}", newData.Count, tableName);

        int insertedCount = 0;
        var totalBatches = (newData.Count + batchSize - 1) / batchSize;

        for (int i = 0; i < newData.Count; i += batchSize)
        {
            var batch = newData.Skip(i).Take(batchSize).ToList();
            var currentBatch = (i / batchSize) + 1;

            logger.LogInformation("📦 Processing batch {Current}/{Total} ({Count} records)",
                currentBatch, totalBatches, batch.Count);

            var valuesParts = new List<string>();
            var parameters = new List<object>();

            for (int j = 0; j < batch.Count; j++)
            {
                var item = batch[j];
                var itemParams = getInsertParameters(item);
                var baseIndex = parameters.Count;

                string paramPlaceholders;
                if (getValuePlaceholders != null)
                {
                    paramPlaceholders = getValuePlaceholders(item);
                    // Replace {0}, {1}, etc. with actual parameter indices
                    for (int k = 0; k < itemParams.Length; k++)
                    {
                        paramPlaceholders = paramPlaceholders.Replace($"{{{k}}}", $"{{{baseIndex + k}}}");
                    }
                }
                else
                {
                    paramPlaceholders = string.Join(", ",
                        Enumerable.Range(baseIndex, itemParams.Length)
                            .Select(idx => $"{{{idx}}}"));
                }

                valuesParts.Add($"({paramPlaceholders})");
                parameters.AddRange(itemParams);
            }

            var insertSql = $@"
                INSERT INTO {tableName} ({insertColumnsClause})
                VALUES {string.Join(", ", valuesParts)};
            ";

            try
            {
                var batchInserted = await context.Database.ExecuteSqlRawAsync(insertSql, parameters.ToArray());
                insertedCount += batchInserted;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to insert batch {Current}/{Total}", currentBatch, totalBatches);
                throw;
            }
        }

        var skippedCount = newData.Count - insertedCount;
        logger.LogInformation("✅ Bulk insert completed: {InsertedCount} inserted, {SkippedCount} skipped",
            insertedCount, skippedCount);

        return (insertedCount, skippedCount);
    }

    /// <summary>
    /// Gets existing records based on composite key for duplicate detection
    /// </summary>
    public static async Task<HashSet<string>> GetExistingCompositeKeys<T>(
        IQueryable<T> queryable,
        Func<T, string> getCompositeKey,
        ILogger logger)
    {
        logger.LogInformation("��� Checking for existing records...");

        var existingRecords = await queryable.ToListAsync();
        var existingSet = new HashSet<string>(existingRecords.Select(getCompositeKey));

        logger.LogInformation("��� Found {Count} existing records in database", existingSet.Count);
        return existingSet;
    }

    /// <summary>
    /// Generates a checksum for data integrity verification
    /// </summary>
    public static string GenerateChecksum(object data)
    {
        var jsonString = JsonSerializer.Serialize(data);
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(jsonString));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
