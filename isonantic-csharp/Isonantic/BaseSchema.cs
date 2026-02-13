namespace Isonantic;

/// <summary>
/// Base interface for all schemas
/// </summary>
public interface ISchema
{
    /// <summary>
    /// Validates a value against this schema
    /// </summary>
    void Validate(object? value);

    /// <summary>
    /// Returns true if the field is optional
    /// </summary>
    bool IsOptional { get; }

    /// <summary>
    /// Gets the default value if one is set
    /// </summary>
    (object? Value, bool HasDefault) GetDefault();

    /// <summary>
    /// Gets the description of this schema
    /// </summary>
    string Description { get; }
}

/// <summary>
/// Provides common functionality for all schemas
/// </summary>
public abstract class BaseSchema : ISchema
{
    public bool IsOptional { get; protected set; }
    protected object? DefaultValue;
    protected bool HasDefaultValue;
    public string Description { get; protected set; } = "";
    protected List<Func<object?, Exception?>> Refinements = new();

    public (object? Value, bool HasDefault) GetDefault()
    {
        return (DefaultValue, HasDefaultValue);
    }

    protected void SetOptional()
    {
        IsOptional = true;
    }

    protected void SetDefault(object? value)
    {
        DefaultValue = value;
        HasDefaultValue = true;
    }

    protected void SetDescription(string desc)
    {
        Description = desc;
    }

    protected void AddRefinement(Func<object?, Exception?> refinement)
    {
        Refinements.Add(refinement);
    }

    protected Exception? RunRefinements(object? value)
    {
        foreach (var refinement in Refinements)
        {
            var error = refinement(value);
            if (error != null)
                return error;
        }
        return null;
    }

    public abstract void Validate(object? value);
}
