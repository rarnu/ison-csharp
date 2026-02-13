namespace Isonantic;

/// <summary>
/// Validates ISON references
/// </summary>
public class RefSchema : BaseSchema
{
    private string? _namespace;
    private string? _relationship;

    public RefSchema Namespace(string ns)
    {
        _namespace = ns;
        return this;
    }

    public RefSchema Relationship(string rel)
    {
        _relationship = rel;
        return this;
    }

    public RefSchema Optional()
    {
        SetOptional();
        return this;
    }

    public RefSchema Describe(string desc)
    {
        SetDescription(desc);
        return this;
    }

    public override void Validate(object? value)
    {
        if (value == null)
        {
            if (IsOptional)
                return;
            throw new Exception("required field is missing");
        }

        switch (value)
        {
            case Dictionary<string, object?> dict:
                // Reference object format
                if (!dict.ContainsKey("_ref"))
                    throw new Exception("expected reference object with _ref field");
                
                if (_namespace != null)
                {
                    if (!dict.TryGetValue("_namespace", out var nsObj) || 
                        nsObj is not string ns || 
                        ns != _namespace)
                        throw new Exception($"expected namespace {_namespace}");
                }
                
                if (_relationship != null)
                {
                    if (!dict.TryGetValue("_relationship", out var relObj) || 
                        relObj is not string rel || 
                        rel != _relationship)
                        throw new Exception($"expected relationship {_relationship}");
                }
                break;
            
            case string str:
                // String reference format (:id, :ns:id, :REL:id)
                if (!str.StartsWith(":"))
                    throw new Exception("expected reference string starting with ':'");
                break;
            
            default:
                throw new Exception($"expected reference, got {value.GetType().Name}");
        }

        var refinementError = RunRefinements(value);
        if (refinementError != null)
            throw refinementError;
    }
}
