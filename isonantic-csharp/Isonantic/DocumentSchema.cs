namespace Isonantic;

/// <summary>
/// Validates complete ISON documents
/// </summary>
public class DocumentSchema
{
    private readonly Dictionary<string, ISchema> _blocks;

    public DocumentSchema(Dictionary<string, ISchema> blocks)
    {
        _blocks = blocks;
    }

    /// <summary>
    /// Validates a document and returns the validated data
    /// </summary>
    public Dictionary<string, object?> Parse(Dictionary<string, object?> value)
    {
        var errors = new List<ValidationError>();

        foreach (var (name, schema) in _blocks)
        {
            var blockValue = value.TryGetValue(name, out var v) ? v : null;
            
            try
            {
                schema.Validate(blockValue);
            }
            catch (ValidationErrors ve)
            {
                foreach (var e in ve.Errors)
                    errors.Add(new ValidationError($"{name}.{e.Field}", e.Message, e.Value));
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(name, ex.Message, blockValue));
            }
        }

        if (errors.Count > 0)
            throw new ValidationErrors(errors);

        return value;
    }

    /// <summary>
    /// Validates without throwing, returns result struct
    /// </summary>
    public SafeParseResult SafeParse(Dictionary<string, object?> value)
    {
        try
        {
            var data = Parse(value);
            return new SafeParseResult(data);
        }
        catch (Exception ex)
        {
            return new SafeParseResult(ex);
        }
    }
}
