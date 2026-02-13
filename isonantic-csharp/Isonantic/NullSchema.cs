namespace Isonantic;

/// <summary>
/// Validates null values
/// </summary>
public class NullSchema : BaseSchema
{
    public override void Validate(object? value)
    {
        if (value != null)
            throw new Exception($"expected null, got {value.GetType().Name}");
    }
}
