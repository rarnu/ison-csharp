using System.Text.RegularExpressions;

namespace Isonantic;

/// <summary>
/// Validates string values
/// </summary>
public class StringSchema : BaseSchema
{
    private int? _minLen;
    private int? _maxLen;
    private int? _exactLen;
    private Regex? _pattern;
    private bool _isEmail;
    private bool _isUrl;

    public StringSchema Min(int n)
    {
        _minLen = n;
        return this;
    }

    public StringSchema Max(int n)
    {
        _maxLen = n;
        return this;
    }

    public StringSchema Length(int n)
    {
        _exactLen = n;
        return this;
    }

    public StringSchema Email()
    {
        _isEmail = true;
        return this;
    }

    public StringSchema Url()
    {
        _isUrl = true;
        return this;
    }

    public StringSchema Regex(Regex pattern)
    {
        _pattern = pattern;
        return this;
    }

    public StringSchema Optional()
    {
        SetOptional();
        return this;
    }

    public StringSchema Default(string v)
    {
        SetDefault(v);
        return this;
    }

    public StringSchema Describe(string desc)
    {
        SetDescription(desc);
        return this;
    }

    public StringSchema Refine(Func<string, bool> fn, string msg)
    {
        AddRefinement(v =>
        {
            if (v is string s && !fn(s))
                return new Exception(msg);
            return null;
        });
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

        if (value is not string str)
            throw new Exception($"expected string, got {value.GetType().Name}");

        if (_minLen.HasValue && str.Length < _minLen.Value)
            throw new Exception($"string must be at least {_minLen.Value} characters");

        if (_maxLen.HasValue && str.Length > _maxLen.Value)
            throw new Exception($"string must be at most {_maxLen.Value} characters");

        if (_exactLen.HasValue && str.Length != _exactLen.Value)
            throw new Exception($"string must be exactly {_exactLen.Value} characters");

        if (_isEmail)
        {
            var emailPattern = new Regex(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
            if (!emailPattern.IsMatch(str))
                throw new Exception("invalid email format");
        }

        if (_isUrl)
        {
            var urlPattern = new Regex(@"^https?://[^\s/$.?#].[^\s]*$");
            if (!urlPattern.IsMatch(str))
                throw new Exception("invalid URL format");
        }

        if (_pattern != null && !_pattern.IsMatch(str))
            throw new Exception("string does not match required pattern");

        var refinementError = RunRefinements(value);
        if (refinementError != null)
            throw refinementError;
    }
}
