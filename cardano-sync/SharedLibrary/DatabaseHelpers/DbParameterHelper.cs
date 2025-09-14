using Npgsql;
using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace SharedLibrary.DatabaseHelpers;

/// <summary>
/// Helper class for creating database parameters
/// </summary>
public static class DbParameterHelper
{
    public static DbParameter CreateParameter(
     DbCommand command,
     string name,
     object? value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;
        param.Value = value ?? DBNull.Value;
        command.Parameters.Add(param);
        return param;
    }

    /// <summary>
    /// Creates a JSONB database parameter for PostgreSQL with optimized performance
    /// </summary>
    /// <param name="command">The database command</param>
    /// <param name="name">Parameter name</param>
    /// <param name="value">Parameter value</param>
    /// <returns>Created JSONB parameter</returns>
    // public static DbParameter CreateJsonbParameter(
    //     DbCommand command,
    //     string name,
    //     object? value)
    // {
    //     var param = command.CreateParameter();
    //     param.ParameterName = name;

    //     if (value == null)
    //     {
    //         param.Value = DBNull.Value;
    //     }
    //     else
    //     {
    //         // Xử lý JsonElement từ database
    //         if (value is JsonElement jsonElement)
    //         {
    //             // Nếu đã là JsonElement, sử dụng trực tiếp
    //             param.Value = jsonElement;
    //         }
    //         else
    //         {
    //             // Luôn serialize value thành JSON mà không kiểm tra parse
    //             param.Value = JsonSerializer.Serialize(value);
    //         }
    //     }

    //     // Set the parameter type to handle JSONB
    //     if (param is NpgsqlParameter npgsqlParam)
    //     {
    //         npgsqlParam.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
    //     }

    //     command.Parameters.Add(param);
    //     return param;
    // }

    public static DbParameter CreateJsonbParameter(
     DbCommand command,
     string name,
     object? value)
    {
        var param = command.CreateParameter();
        param.ParameterName = name;

        if (value == null)
        {
            param.Value = DBNull.Value;
        }
        else
        {
            if (value is JsonElement jsonElement)
            {
                param.Value = jsonElement;
            }
            else if (value is string stringValue)
            {
                // Bước 1: Kiểm tra nhanh trước
                if (!IsLikelyJsonString(stringValue))
                {
                    param.Value = JsonSerializer.Serialize(stringValue);
                }
                else
                {
                    // Bước 2: Validation nhẹ với Utf8JsonReader
                    try
                    {
                        var bytes = Encoding.UTF8.GetBytes(stringValue);
                        var reader = new Utf8JsonReader(bytes);

                        // Chỉ đọc token đầu tiên để validate
                        if (reader.Read())
                        {
                            param.Value = stringValue; // Valid JSON
                        }
                        else
                        {
                            param.Value = JsonSerializer.Serialize(stringValue);
                        }
                    }
                    catch
                    {
                        param.Value = JsonSerializer.Serialize(stringValue);
                    }
                }
            }
            else
            {
                param.Value = JsonSerializer.Serialize(value);
            }
        }

        if (param is NpgsqlParameter npgsqlParam)
        {
            npgsqlParam.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Jsonb;
        }

        command.Parameters.Add(param);
        return param;
    }

    // Validation với Utf8JsonReader (nhanh hơn JsonDocument.Parse)
    private static bool IsLikelyJsonString(string str)
    {
        if (string.IsNullOrWhiteSpace(str))
            return false;

        var trimmed = str.AsSpan().Trim();
        if (trimmed.Length < 2)
            return false;

        var first = trimmed[0];
        var last = trimmed[trimmed.Length - 1];

        // Kiểm tra pattern cơ bản của JSON
        return (first == '{' && last == '}') ||
               (first == '[' && last == ']') ||
               (first == '"' && last == '"');
    }
}