namespace Isonantic;

/// <summary>
/// Isonantic version information
/// </summary>
public static class IsonanticInfo
{
    /// <summary>
    /// Current version of the Isonantic package
    /// </summary>
    public const string Version = "1.0.0";
}

/// <summary>
/// Provides a namespace for schema creation (like Zod's z)
/// </summary>
public static class I
{
    public static StringSchema String() => new();
    public static NumberSchema Number() => new();
    public static NumberSchema Int()
    {
        var schema = new NumberSchema();
        schema.SetInt(true);
        return schema;
    }
    public static NumberSchema Float() => new();
    public static BooleanSchema Boolean() => new();
    public static BooleanSchema Bool() => new();
    public static NullSchema Null() => new();
    public static RefSchema Ref() => new();
    public static RefSchema Reference() => new();
    public static ObjectSchema Object(Dictionary<string, ISchema> fields) => new(fields);
    public static ArraySchema Array(ISchema itemSchema) => new(itemSchema);
    public static TableSchema Table(string name, Dictionary<string, ISchema> fields) => new(name, fields);
    public static DocumentSchema Document(Dictionary<string, ISchema> blocks) => new(blocks);
}
