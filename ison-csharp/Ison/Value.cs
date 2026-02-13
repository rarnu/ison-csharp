namespace Ison
{
    /// <summary>
    /// Represents an ISON value which can be null, bool, int, float, string, or reference
    /// </summary>
    public struct Value : IEquatable<Value>
    {
        public ValueType Type { get; set; }
        public bool BoolVal { get; set; }
        public long IntVal { get; set; }
        public double FloatVal { get; set; }
        public string StringVal { get; set; }
        public Reference RefVal { get; set; }

        public static Value Null() => new() { Type = ValueType.Null };
        public static Value Bool(bool v) => new() { Type = ValueType.Bool, BoolVal = v };
        public static Value Int(long v) => new() { Type = ValueType.Int, IntVal = v };
        public static Value Float(double v) => new() { Type = ValueType.Float, FloatVal = v };
        public static Value String(string v) => new() { Type = ValueType.String, StringVal = v };
        public static Value Ref(Reference r) => new() { Type = ValueType.Reference, RefVal = r };

        public bool IsNull()
        {
            return Type == ValueType.Null;
        }

        public (bool value, bool ok) AsBool()
        {
            if (Type == ValueType.Bool)
            {
                return (BoolVal, true);
            }
            return (false, false);
        }

        public (long value, bool ok) AsInt()
        {
            if (Type == ValueType.Int)
            {
                return (IntVal, true);
            }
            return (0, false);
        }

        public (double value, bool ok) AsFloat()
        {
            if (Type == ValueType.Float)
            {
                return (FloatVal, true);
            }
            if (Type == ValueType.Int)
            {
                return (IntVal, true);
            }
            return (0, false);
        }

        public (string value, bool ok) AsString()
        {
            if (Type == ValueType.String)
            {
                return (StringVal, true);
            }
            return ("", false);
        }

        public (Reference value, bool ok) AsRef()
        {
            if (Type == ValueType.Reference)
            {
                return (RefVal, true);
            }
            return (new Reference(), false);
        }

        public object? ToObject()
        {
            return Type switch
            {
                ValueType.Null => null,
                ValueType.Bool => BoolVal,
                ValueType.Int => IntVal,
                ValueType.Float => FloatVal,
                ValueType.String => StringVal,
                ValueType.Reference => RefVal,
                _ => null
            };
        }

        public string ToIson()
        {
            return Type switch
            {
                ValueType.Null => "~",
                ValueType.Bool => BoolVal ? "true" : "false",
                ValueType.Int => IntVal.ToString(),
                ValueType.Float => FloatVal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ValueType.String => EscapeString(StringVal),
                ValueType.Reference => RefVal.ToIson(),
                _ => "~"
            };
        }

        private static string EscapeString(string str)
        {
            if (string.IsNullOrEmpty(str) || str.Any(c => c == ' ' || c == '\t' || c == '\n' || c == '"'))
            {
                var escaped = str
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
                return $"\"{escaped}\"";
            }
            return str;
        }

        public bool Equals(Value other)
        {
            if (Type != other.Type) return false;
            return Type switch
            {
                ValueType.Null => true,
                ValueType.Bool => BoolVal == other.BoolVal,
                ValueType.Int => IntVal == other.IntVal,
                ValueType.Float => Math.Abs(FloatVal - other.FloatVal) < 0.00001,
                ValueType.String => StringVal == other.StringVal,
                ValueType.Reference => RefVal.Equals(other.RefVal),
                _ => false
            };
        }

        public override bool Equals(object? obj)
        {
            return obj is Value other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Type switch
            {
                ValueType.Null => 0,
                ValueType.Bool => BoolVal.GetHashCode(),
                ValueType.Int => IntVal.GetHashCode(),
                ValueType.Float => FloatVal.GetHashCode(),
                ValueType.String => StringVal?.GetHashCode() ?? 0,
                ValueType.Reference => RefVal.GetHashCode(),
                _ => 0
            };
        }

        public static bool operator ==(Value left, Value right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Value left, Value right)
        {
            return !(left == right);
        }
    }
}
