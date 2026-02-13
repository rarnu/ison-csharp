namespace Isonantic;

/// <summary>
/// Represents a single validation error with field path and message
/// </summary>
public class ValidationError : Exception
{
    /// <summary>
    /// The field path where the error occurred
    /// </summary>
    public string Field { get; }

    /// <summary>
    /// The value that failed validation
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Creates a new validation error
    /// </summary>
    public ValidationError(string field, string message, object? value = null)
        : base(message)
    {
        Field = field;
        Value = value;
    }

    /// <summary>
    /// Returns a string representation of the error
    /// </summary>
    public override string ToString()
    {
        return $"{Field}: {Message}";
    }
}

/// <summary>
/// Represents a collection of validation errors
/// </summary>
public class ValidationErrors : Exception
{
    /// <summary>
    /// The list of validation errors
    /// </summary>
    public List<ValidationError> Errors { get; }

    /// <summary>
    /// Creates a new validation errors collection
    /// </summary>
    public ValidationErrors(List<ValidationError> errors)
        : base(string.Join("; ", errors.Select(e => e.ToString())))
    {
        Errors = errors;
    }

    /// <summary>
    /// Creates a new validation errors collection from an enumerable
    /// </summary>
    public ValidationErrors(IEnumerable<ValidationError> errors)
        : this(errors.ToList())
    {
    }

    /// <summary>
    /// Creates an empty validation errors collection
    /// </summary>
    public ValidationErrors()
        : this(new List<ValidationError>())
    {
    }

    /// <summary>
    /// Returns true if there are any validation errors
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Adds an error to the collection
    /// </summary>
    public void Add(ValidationError error)
    {
        Errors.Add(error);
    }
}

/// <summary>
/// Contains the result of SafeParse operation
/// </summary>
public class SafeParseResult
{
    /// <summary>
    /// Whether the validation succeeded
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// The validated data (only set if Success is true)
    /// </summary>
    public Dictionary<string, object?>? Data { get; }

    /// <summary>
    /// The validation error (only set if Success is false)
    /// </summary>
    public Exception? Error { get; }

    /// <summary>
    /// Creates a successful parse result
    /// </summary>
    public SafeParseResult(Dictionary<string, object?> data)
    {
        Success = true;
        Data = data;
        Error = null;
    }

    /// <summary>
    /// Creates a failed parse result
    /// </summary>
    public SafeParseResult(Exception error)
    {
        Success = false;
        Data = null;
        Error = error;
    }
}
