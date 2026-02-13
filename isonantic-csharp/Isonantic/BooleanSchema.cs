namespace Isonantic;

/// <summary>
/// Validates boolean values
/// </summary>
public class BooleanSchema : BaseSchema
{
    public BooleanSchema Optional()
    {
        SetOptional();
        return this;
    }

    public BooleanSchema Default(bool v)
    {
        SetDefault(v);
        return this;
    }

    public BooleanSchema Describe(string desc)
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

        if (value is not bool)
            throw new Exception($"expected boolean, got {value.GetType().Name}");

        var refinementError = RunRefinements(value);
        if (refinementError != null)
            throw refinementError;
    }
}
