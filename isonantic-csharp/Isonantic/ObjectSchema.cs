namespace Isonantic;

/// <summary>
/// Validates object structures
/// </summary>
public class ObjectSchema : BaseSchema
{
    private readonly Dictionary<string, ISchema> _fields;

    public ObjectSchema(Dictionary<string, ISchema> fields)
    {
        _fields = fields;
    }

    public ObjectSchema Optional()
    {
        SetOptional();
        return this;
    }

    public ObjectSchema Describe(string desc)
    {
        SetDescription(desc);
        return this;
    }

    public ObjectSchema Extend(Dictionary<string, ISchema> fields)
    {
        var newFields = new Dictionary<string, ISchema>(_fields);
        foreach (var kv in fields)
            newFields[kv.Key] = kv.Value;
        return new ObjectSchema(newFields);
    }

    public ObjectSchema Pick(params string[] keys)
    {
        var newFields = new Dictionary<string, ISchema>();
        foreach (var key in keys)
        {
            if (_fields.TryGetValue(key, out var schema))
                newFields[key] = schema;
        }
        return new ObjectSchema(newFields);
    }

    public ObjectSchema Omit(params string[] keys)
    {
        var keySet = new HashSet<string>(keys);
        var newFields = new Dictionary<string, ISchema>();
        foreach (var kv in _fields)
        {
            if (!keySet.Contains(kv.Key))
                newFields[kv.Key] = kv.Value;
        }
        return new ObjectSchema(newFields);
    }

    public override void Validate(object? value)
    {
        if (value == null)
        {
            if (IsOptional)
                return;
            throw new Exception("required field is missing");
        }

        if (value is not Dictionary<string, object?> obj)
            throw new Exception($"expected object, got {value.GetType().Name}");

        var errors = new List<ValidationError>();
        foreach (var (name, schema) in _fields)
        {
            var fieldValue = obj.TryGetValue(name, out var v) ? v : null;
            
            if (fieldValue == null && !schema.IsOptional)
            {
                var (defVal, hasDefault) = schema.GetDefault();
                if (hasDefault)
                {
                    obj[name] = defVal;
                    continue;
                }
            }

            try
            {
                schema.Validate(fieldValue);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError(name, ex.Message, fieldValue));
            }
        }

        if (errors.Count > 0)
            throw new ValidationErrors(errors);

        var refinementError = RunRefinements(value);
        if (refinementError != null)
            throw refinementError;
    }
}
