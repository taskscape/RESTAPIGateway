using System.Text.Json;

namespace GenericTableAPI.Utilities;

/// <summary>
/// Utility class for converting JsonElement objects to appropriate .NET types for database operations
/// </summary>
public static class JsonElementConverter
{
    /// <summary>
    /// Converts an IDictionary with JsonElement values to an IDictionary with proper .NET types
    /// </summary>
    /// <param name="source">Dictionary potentially containing JsonElement values</param>
    /// <returns>Dictionary with converted .NET type values</returns>
    public static IDictionary<string, object?> ConvertJsonElements(IDictionary<string, object?> source)
    {
        var result = new Dictionary<string, object?>();

        foreach (var kvp in source)
        {
            result[kvp.Key] = ConvertJsonElementValue(kvp.Value);
        }

        return result;
    }

    /// <summary>
    /// Converts a single JsonElement value to its appropriate .NET type
    /// </summary>
    /// <param name="value">Value to convert</param>
    /// <returns>Converted value</returns>
    private static object? ConvertJsonElementValue(object? value)
    {
        if (value == null)
            return null;

        if (value is JsonElement jsonElement)
        {
            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => TryGetNumber(jsonElement),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Undefined => null,
                _ => jsonElement.ToString()
            };
        }

        // If it's not a JsonElement, return as-is
        return value;
    }

    /// <summary>
    /// Attempts to convert a JsonElement number to the most appropriate .NET numeric type
    /// </summary>
    /// <param name="element">JsonElement containing a number</param>
    /// <returns>Int32, Int64, Double, or Decimal depending on the value</returns>
    private static object TryGetNumber(JsonElement element)
    {
        // Try int first
        if (element.TryGetInt32(out int intValue))
            return intValue;

        // Try long
        if (element.TryGetInt64(out long longValue))
            return longValue;

        // Try double
        if (element.TryGetDouble(out double doubleValue))
            return doubleValue;

        // Try decimal
        if (element.TryGetDecimal(out decimal decimalValue))
            return decimalValue;

        // Fallback to string representation
        return element.ToString();
    }
}
