namespace Isonantic;

/// <summary>
/// Validates arrays
/// </summary>
public class ArraySchema : BaseSchema
{
    private readonly ISchema _itemSchema;
    private int? _minLen;
    private int? _maxLen;

    public ArraySchema(ISchema itemSchema)
    {
        _itemSchema = itemSchema;
    }

    public ArraySchema Min(int n)
    {
        _minLen = n;
        return this;
    }

    public ArraySchema Max(int n)
    {
        _maxLen = n;
        return this;
    }

    public ArraySchema Optional()
    {
        SetOptional();
        return this;
    }

    public ArraySchema Describe(string desc)
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

        if (value is not List<object?> arr)
            throw new Exception($"expected array, got {value.GetType().Name}");

        if (_minLen.HasValue && arr.Count < _minLen.Value)
            throw new Exception($"array must have at least {_minLen.Value} items");

        if (_maxLen.HasValue && arr.Count > _maxLen.Value)
            throw new Exception($"array must have at most {_maxLen.Value} items");

        var errors = new List<ValidationError>();
        for (int i = 0; i < arr.Count; i++)
        {
            try
            {
                _itemSchema.Validate(arr[i]);
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError($"[{i}]", ex.Message, arr[i]));
            }
        }

        if (errors.Count > 0)
            throw new ValidationErrors(errors);

        var refinementError = RunRefinements(value);
        if (refinementError != null)
            throw refinementError;
    }
}
