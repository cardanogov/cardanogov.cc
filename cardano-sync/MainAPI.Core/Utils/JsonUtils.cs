using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using System.Text.Json;

namespace MainAPI.Core.Utils
{
    /// <summary>
    /// Utility class for JSON parsing operations commonly used across the application
    /// </summary>
    public static class JsonUtils
    {
        /// <summary>
        /// Safely parses a JSON string to an integer value
        /// </summary>
        /// <param name="jsonValue">The JSON string to parse</param>
        /// <returns>The parsed integer value, or null if parsing fails</returns>
        public static int? ParseJsonInt(string? jsonValue)
        {
            if (string.IsNullOrEmpty(jsonValue)) return null;
            try
            {
                return JsonConvert.DeserializeObject<int>(jsonValue);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely parses a JSON string to a double value
        /// </summary>
        /// <param name="jsonValue">The JSON string to parse</param>
        /// <returns>The parsed double value, or null if parsing fails</returns>
        public static double? ParseJsonDouble(string? jsonValue)
        {
            if (string.IsNullOrEmpty(jsonValue)) return null;
            try
            {
                return JsonConvert.DeserializeObject<double>(jsonValue);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely parses a JSON string to a Dictionary of string keys and List of integers
        /// </summary>
        /// <param name="jsonValue">The JSON string to parse</param>
        /// <returns>The parsed dictionary, or null if parsing fails</returns>
        public static Dictionary<string, List<int>>? ParseJsonCostModels(string? jsonValue)
        {
            if (string.IsNullOrEmpty(jsonValue)) return null;
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, List<int>>>(jsonValue);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely parses a JSON string to a generic type
        /// </summary>
        /// <typeparam name="T">The target type to deserialize to</typeparam>
        /// <param name="jsonValue">The JSON string to parse</param>
        /// <returns>The parsed object of type T, or null if parsing fails</returns>
        public static T? ParseJson<T>(string? jsonValue)
        {
            if (string.IsNullOrEmpty(jsonValue)) return default(T);
            try
            {
                return JsonConvert.DeserializeObject<T>(jsonValue);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// Extracts the title from a proposal's meta JSON
        /// </summary>
        /// <param name="metaJson">The meta JSON string containing proposal information</param>
        /// <returns>The extracted title, or null if not found or parsing fails</returns>
        public static string? ExtractTitleFromMetaJson(string? metaJson)
        {
            if (string.IsNullOrEmpty(metaJson)) return null;
            try
            {
                var meta = JsonConvert.DeserializeObject<dynamic>(metaJson);
                return meta?.body?.title?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely deserializes a JSON string to a List of objects
        /// </summary>
        /// <param name="jsonValue">The JSON string to parse</param>
        /// <returns>The parsed list, or null if parsing fails</returns>
        public static List<object>? ParseJsonList(string? jsonValue)
        {
            if (string.IsNullOrEmpty(jsonValue)) return null;
            try
            {
                return JsonConvert.DeserializeObject<List<object>>(jsonValue);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parses a JSONB decimal field by trimming quotes and converting to decimal
        /// </summary>
        /// <param name="jsonbValue">The JSONB string value (e.g., "\"123.45\"")</param>
        /// <returns>The parsed decimal value, or null if parsing fails</returns>
        public static decimal? ParseJsonBDecimal(string? jsonbValue)
        {
            if (string.IsNullOrEmpty(jsonbValue)) return null;
            try
            {
                var cleanValue = jsonbValue.Trim('"');
                if (decimal.TryParse(cleanValue, out var result))
                    return result;
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parses a JSONB string field by extracting nested JSON properties
        /// </summary>
        /// <param name="jsonbValue">The JSONB string value</param>
        /// <param name="propertyPath">The property path to extract (e.g., "body.title")</param>
        /// <returns>The extracted string value, or null if not found or parsing fails</returns>
        public static string? ParseJsonBString(string? jsonbValue, string propertyPath)
        {
            if (string.IsNullOrEmpty(jsonbValue)) return null;
            try
            {
                var meta = JsonConvert.DeserializeObject<dynamic>(jsonbValue);
                if (meta == null) return null;

                var properties = propertyPath.Split('.');
                dynamic current = meta;

                foreach (var prop in properties)
                {
                    if (current == null) return null;
                    current = current[prop];
                }

                return current?.ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Parses references from JSONB meta_json field
        /// </summary>
        /// <param name="jsonbValue">The JSONB string value</param>
        /// <param name="logger">Optional logger for debugging</param>
        /// <returns>List of ReferencesDto, or empty list if parsing fails</returns>
        public static string? ParseJsonBReferences(string? jsonbValue, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(jsonbValue)) return "";

            try
            {
                var meta = JsonConvert.DeserializeObject<dynamic>(jsonbValue);
                if (meta?.body?.references == null) return "";

                return JsonConvert.SerializeObject(meta.body.references);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to parse references from JSONB: {Value}", jsonbValue);
                return "";
            }
        }

        /// <summary>
        /// Parses references from a list of JSONB meta_json field values and combines them into a single JSON string
        /// </summary>
        /// <param name="jsonbValues">List of JSONB string values</param>
        /// <param name="logger">Optional logger for debugging</param>
        /// <returns>A JSON string containing a list of all references, or an empty JSON array if parsing fails</returns>
        public static string ParseJsonBReferencesList(IList<string?> jsonbValues, ILogger? logger = null)
        {
            if (jsonbValues == null || jsonbValues.Count == 0) return "[]";

            var allReferences = new List<object>();

            foreach (var jsonbValue in jsonbValues)
            {
                if (string.IsNullOrEmpty(jsonbValue)) continue;

                try
                {
                    var meta = JsonConvert.DeserializeObject<dynamic>(jsonbValue);
                    if (meta?.body?.references != null)
                    {
                        var references = JsonConvert.DeserializeObject<List<object>>(JsonConvert.SerializeObject(meta.body.references));
                        if (references != null)
                        {
                            allReferences.AddRange(references);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to parse references from JSONB: {Value}", jsonbValue);
                }
            }

            return JsonConvert.SerializeObject(allReferences);
        }

        public static string? ParseJsonBImage(string? jsonbValue, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(jsonbValue)) return "";

            try
            {
                var meta = JsonConvert.DeserializeObject<dynamic>(jsonbValue);
                if (meta?.body?.image?.contentUrl == null) return "";

                string result = meta.body.image.contentUrl;
                return result;
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to parse references from JSONB: {Value}", jsonbValue);
                return "";
            }
        }

        public static string? ParseJsonBTitle(string? jsonbValue, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(jsonbValue)) return null;

            try
            {
                var meta = JsonConvert.DeserializeObject<dynamic>(jsonbValue);
                if (meta?.body?.title == null) return null;

                string result = meta.body.title;
                return result.Trim('"');
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to parse title from JSONB: {Value}", jsonbValue);
                return null;
            }
        }

        public static string? ParseJsonBPoolTicker(string? jsonbValue, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(jsonbValue)) return null;

            try
            {
                var meta = JsonConvert.DeserializeObject<dynamic>(jsonbValue);
                if (meta?.ticker == null) return null;

                string result = meta.ticker;
                return result.Trim('"');
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to parse title from JSONB: {Value}", jsonbValue);
                return null;
            }
        }

        public static string? ParseJsonBSubTitle(string? jsonbValue, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(jsonbValue)) return null;

            try
            {
                var meta = JsonConvert.DeserializeObject<dynamic>(jsonbValue);
                if (meta?.body?.title == null) return null;

                string result = meta.body.title;
                return result.Trim('"');
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to parse title from JSONB: {Value}", jsonbValue);
                return null;
            }
        }

        public static string? ParseJsonBAbstract(string? jsonbValue, ILogger? logger = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonbValue)) return null;
                using var doc = JsonDocument.Parse(jsonbValue);
                var root = doc.RootElement;

                if (root.TryGetProperty("body", out var body))
                {
                    if (body.TryGetProperty("abstract", out var givenName))
                    {
                        if (givenName.ValueKind == JsonValueKind.String)
                        {
                            return givenName.GetString();
                        }
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string? ParseJsonBMotivation(string? jsonbValue, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(jsonbValue)) return "";

            try
            {
                var meta = JsonConvert.DeserializeObject<dynamic>(jsonbValue);
                if (meta?.body?.motivation == null) return "";

                string result = meta.body.motivation;
                return result.Trim('"');
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to parse motivation from JSONB: {Value}", jsonbValue);
                return "";
            }
        }

        public static string? ParseJsonBRationale(string? jsonbValue, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(jsonbValue)) return "";

            try
            {
                var meta = JsonConvert.DeserializeObject<dynamic>(jsonbValue);
                if (meta?.body?.rationale == null) return "";

                string result = meta.body.rationale;
                return result.Trim('"');
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to parse rationale from JSONB: {Value}", jsonbValue);
                return "";
            }
        }

        /// <summary>
        /// Formats JSONB fields by removing extra quotes and parsing JSON if needed
        /// </summary>
        /// <param name="jsonBValue">The JSONB value from database</param>
        /// <param name="logger">Optional logger for debugging</param>
        /// <returns>Formatted string value</returns>
        public static string FormatJsonBField(string? jsonBValue, ILogger? logger = null)
        {
            if (string.IsNullOrEmpty(jsonBValue))
                return "";

            logger?.LogDebug("Original JSONB value: {Value}", jsonBValue);

            // Remove surrounding quotes if they exist
            var trimmed = jsonBValue.Trim();
            if (trimmed.StartsWith("\"") && trimmed.EndsWith("\""))
            {
                trimmed = trimmed[1..^1]; // Remove first and last character (quotes)
            }

            logger?.LogDebug("After trimming quotes: {Value}", trimmed);

            // If the value looks like JSON, try to parse and format it
            if (trimmed.StartsWith("{") || trimmed.StartsWith("["))
            {
                try
                {
                    // First, validate it's valid JSON
                    using var doc = JsonDocument.Parse(trimmed);

                    // Return the trimmed JSON string as-is (already valid JSON)
                    return trimmed;
                }
                catch (Exception ex)
                {
                    logger?.LogWarning(ex, "Failed to parse JSONB field as JSON: {Value}", trimmed);
                    // If it's not valid JSON, return as string
                    return trimmed;
                }
            }

            // For non-JSON values, return trimmed value
            return trimmed;
        }

        /// <summary>
        /// get given name from meta_json
        /// </summary>
        /// <param name="jsonString"></param>
        /// <returns></returns>
        public static string? ParseGivenName(string? jsonString)
        {
            if (string.IsNullOrWhiteSpace(jsonString)) return null;

            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(jsonString));

            string? propertyName = null;
            bool insideBody = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString();

                    if (propertyName == "body")
                        insideBody = true;
                }
                else if (insideBody && propertyName == "givenName")
                {
                    if (reader.TokenType == JsonTokenType.String)
                        return reader.GetString();

                    if (reader.TokenType == JsonTokenType.StartObject)
                    {
                        // đọc tiếp để tìm @value
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonTokenType.PropertyName && reader.GetString() == "@value")
                            {
                                reader.Read();
                                if (reader.TokenType == JsonTokenType.String)
                                    return reader.GetString();
                            }
                            else if (reader.TokenType == JsonTokenType.EndObject)
                            {
                                break;
                            }
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Safely parses objectives from JSON metadata
        /// </summary>
        public static string? ParseObjectives(string? jsonString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonString)) return null;
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty("body", out var body) &&
                    body.TryGetProperty("objectives", out var objectives))
                {
                    return objectives.GetString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely parses motivations from JSON metadata
        /// </summary>
        public static string? ParseMotivations(string? jsonString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonString)) return null;
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty("body", out var body) &&
                    body.TryGetProperty("motivations", out var motivations))
                {
                    return motivations.GetString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely parses qualifications from JSON metadata
        /// </summary>
        public static string? ParseQualifications(string? jsonString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonString)) return null;
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty("body", out var body) &&
                    body.TryGetProperty("qualifications", out var qualifications))
                {
                    return qualifications.GetString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely parses references array from JSON metadata
        /// </summary>
        public static string? ParseReferences(string? jsonString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonString)) return null;
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty("body", out var body) &&
                    body.TryGetProperty("references", out var references))
                {

                    return references.ToString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Safely parses image URL from JSON metadata
        /// </summary>
        public static string? ParseImageUrl(string? jsonString)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(jsonString)) return null;
                using var doc = JsonDocument.Parse(jsonString);
                var root = doc.RootElement;

                if (root.TryGetProperty("body", out var body) &&
                    body.TryGetProperty("image", out var image) &&
                    image.TryGetProperty("contentUrl", out var contentUrl))
                {
                    return contentUrl.GetString();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        public static string? ParseHomepage(string? metaJson)
        {
            if (string.IsNullOrWhiteSpace(metaJson)) return null;

            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(metaJson));
            string? propertyName = null;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    propertyName = reader.GetString();
                }
                else if (propertyName == "homepage")
                {
                    if (reader.TokenType == JsonTokenType.String)
                        return reader.GetString();
                }
            }

            return null;
        }

    }
}