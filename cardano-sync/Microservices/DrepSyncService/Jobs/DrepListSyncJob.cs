using DrepSyncService.ApiResponses;
using DrepSyncService.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;
using SharedLibrary.DatabaseContext;
using System.Data;

namespace DrepSyncService.Jobs;

[DisallowConcurrentExecution]
public class DrepListSyncJob : IJob
{
    private readonly CardanoDbContext _context;
    private readonly DatabaseSyncService _databaseSyncService;
    private readonly DatabaseHealthCheckService _healthCheckService;
    private readonly ILogger<DrepListSyncJob> _logger;
    private const string ServiceName = "DrepSyncService";

    public DrepListSyncJob(
        CardanoDbContext context,
        DatabaseSyncService databaseSyncService,
        DatabaseHealthCheckService healthCheckService,
        ILogger<DrepListSyncJob> logger)
    {
        _context = context;
        _databaseSyncService = databaseSyncService;
        _healthCheckService = healthCheckService;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        var startTime = DateTime.Now;
        _logger.LogInformation("🚀 DrepListSyncJob started at {StartTime}. Trigger: {Trigger}",
            startTime, context.Trigger.Key.Name);

        try
        {
            // Test connection to backup databases before proceeding
            _logger.LogInformation("🔍 Testing connection to backup databases...");
            var healthResult = await _healthCheckService.CheckAllDatabasesAsync();

            if (healthResult.OverallStatus != "Healthy")
            {
                _logger.LogError("❌ All backup databases are unhealthy. Cannot proceed with sync.");
                throw new InvalidOperationException("All backup databases are unavailable");
            }

            _logger.LogInformation("✅ Backup database connection test successful");

            // Fetch all drep list data from backup database
            var allData = await _databaseSyncService.GetDrepListAsync();

            _logger.LogInformation("📊 Fetched {Count} records from backup database",
                allData.Length);

            _logger.LogInformation("📝 Processing {Count} valid records", allData.Length);

            if (allData.Length == 0)
            {
                var completeDuration = DateTime.Now - startTime;
                _logger.LogInformation("✅ DrepListSyncJob completed - no data to process! Duration: {Duration}. Trigger: {Trigger}",
                    completeDuration, context.Trigger.Key.Name);
                return;
            }

            await BulkRefreshDrepList(allData.ToList());

            var finalDuration = DateTime.Now - startTime;

            _logger.LogInformation("✅ DrepListSyncJob completed successfully at {EndTime}! Duration: {Duration}. " +
                "Processed {FetchedCount} records from backup database. Trigger: {Trigger}",
                DateTime.Now, finalDuration, allData.Length, context.Trigger.Key.Name);
        }
        catch (Exception ex)
        {
            var errorDuration = DateTime.Now - startTime;
            _logger.LogError(ex, "❌ DrepListSyncJob failed at {EndTime}! Duration: {Duration}. " +
                "Error: {ErrorMessage}. Trigger: {Trigger}",
                DateTime.Now, errorDuration, ex.Message, context.Trigger.Key.Name);
            throw;
        }
    }

    private async Task BulkRefreshDrepList(List<DrepListApiResponse> records)
    {
        _logger.LogInformation("🔄 Starting full refresh for {Count} drep list records", records.Count);

        try
        {
            // Step 1: Delete all existing records
            await DeleteAllDrepListRecords();

            // Step 2: Insert all new records in batches
            const int batchSize = 500;
            var totalBatches = (int)Math.Ceiling((double)records.Count / batchSize);

            for (int batchIndex = 0; batchIndex < totalBatches; batchIndex++)
            {
                var batch = records.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                await InsertBatchWithRawSql(batch, batchIndex + 1, totalBatches);
            }

            _logger.LogInformation("✅ Full refresh completed successfully for {Count} records", records.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error during full refresh: {Message}", ex.Message);
            throw;
        }
    }

    private async Task DeleteAllDrepListRecords()
    {
        _logger.LogInformation("🗑️ Deleting all existing drep list records...");
        var deleteCount = await _context.Database.ExecuteSqlRawAsync("DELETE FROM md_dreps_list");
        _logger.LogInformation("🗑️ Deleted {Count} existing drep list records", deleteCount);
    }

    private async Task InsertBatchWithRawSql(List<DrepListApiResponse> batch, int batchNumber, int totalBatches)
    {
        _logger.LogInformation("📝 Inserting batch {BatchNumber}/{TotalBatches} with {Count} records",
            batchNumber, totalBatches, batch.Count);

        var values = string.Join(",", batch.Select((_, index) => $"(@drepId{batchNumber}_{index}, @hex{batchNumber}_{index}, @hasScript{batchNumber}_{index}, @registered{batchNumber}_{index})"));

        var sql = $@"
            INSERT INTO md_dreps_list (drep_id, hex, has_script, registered)
            VALUES {values}
            ON CONFLICT (drep_id) DO UPDATE SET 
              hex = EXCLUDED.hex,
              has_script = EXCLUDED.has_script,
              registered = EXCLUDED.registered";

        using var command = _context.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.CommandType = CommandType.Text;

        for (int i = 0; i < batch.Count; i++)
        {
            var record = batch[i];
            CreateParameter(command, $"@drepId{batchNumber}_{i}", record.drep_id);
            CreateParameter(command, $"@hex{batchNumber}_{i}", record.hex);
            CreateParameter(command, $"@hasScript{batchNumber}_{i}", record.has_script);
            CreateParameter(command, $"@registered{batchNumber}_{i}", record.registered);
        }

        await _context.Database.GetDbConnection().OpenAsync();
        await command.ExecuteNonQueryAsync();
        await _context.Database.GetDbConnection().CloseAsync();

        _logger.LogInformation("✅ Successfully inserted batch {BatchNumber}/{TotalBatches}", batchNumber, totalBatches);
    }

    private System.Data.Common.DbParameter CreateParameter(
        System.Data.Common.DbCommand command,
        string name,
        object? value
    )
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
        return parameter;
    }
}
