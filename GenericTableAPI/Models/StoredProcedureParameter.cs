namespace GenericTableAPI.Models;

public class StoredProcedureParameter
{
    /// <summary>
    /// The name of the parameter
    /// </summary>
    /// <example>Param1</example>
    public string Name { get; set; }
    
    
    /// <summary>
    /// The value of the parameter
    /// </summary>
    /// <example>sample_value</example>
    public string Value { get; set; }
    
    /// <summary>
    /// The type of the parameter. The only allowed types are `string`, `int`, `float` and `null`. For type `null`, `value` is not taken into account
    /// </summary>
    /// <example>string</example>
    public string Type { get; set; }
}