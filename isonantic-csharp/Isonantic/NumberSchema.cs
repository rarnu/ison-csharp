namespace Isonantic;

/// <summary>
/// Validates numeric values
/// </summary>
public class NumberSchema : BaseSchema
{
    private double? _minVal;
    private double? _maxVal;
    private bool _isInt;
    private bool _isPositive;
    private bool _isNegative;

    internal void SetInt(bool isInt) => _isInt = isInt;

    public NumberSchema Min(double n)
    {
        _minVal = n;
        return this;
    }

    public NumberSchema Max(double n)
    {
        _maxVal = n;
        return this;
    }

    public NumberSchema Positive()
    {
        _isPositive = true;
        return this;
    }

    public NumberSchema Negative()
    {
        _isNegative = true;
        return this;
    }

    public NumberSchema Optional()
    {
        SetOptional();
        return this;
    }

    public NumberSchema Default(double v)
    {
        SetDefault(v);
        return this;
    }

    public NumberSchema Describe(string desc)
    {
        SetDescription(desc);
        return this;
    }

    public NumberSchema Refine(Func<double, bool> fn, string msg)
    {
        AddRefinement(v =>
        {
            double num = v switch
            {
                double d => d,
                long l => l,
                int i => i,
                _ => double.NaN
            };

            if (!double.IsNaN(num) && !fn(num))
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

        double num = value switch
        {
            double d => d,
            long l => l,
            int i => i,
            _ => double.NaN
        };

        if (double.IsNaN(num))
            throw new Exception($"expected number, got {value.GetType().Name}");

        if (_isInt)
        {
            if (Math.Abs(num - (long)num) > double.Epsilon)
                throw new Exception("expected integer, got float");
        }

        if (_minVal.HasValue && num < _minVal.Value)
            throw new Exception($"number must be at least {_minVal.Value}");

        if (_maxVal.HasValue && num > _maxVal.Value)
            throw new Exception($"number must be at most {_maxVal.Value}");

        if (_isPositive && num <= 0)
            throw new Exception("number must be positive");

        if (_isNegative && num >= 0)
            throw new Exception("number must be negative");

        var refinementError = RunRefinements(value);
        if (refinementError != null)
            throw refinementError;
    }
}
