namespace Isonantic;

/// <summary>
/// Validates ISON table blocks
/// </summary>
public class TableSchema : BaseSchema
{
    private readonly string _name;
    private readonly Dictionary<string, ISchema> _fields;
    private readonly ObjectSchema _rowSchema;

    public TableSchema(string name, Dictionary<string, ISchema> fields)
    {
        _name = name;
        _fields = fields;
        _rowSchema = new ObjectSchema(fields);
    }

    public string GetName() => _name;

    public TableSchema Optional()
    {
        SetOptional();
        return this;
    }

    public TableSchema Describe(string desc)
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
            throw new Exception("required table is missing");
        }

        // Value could be block dict or array of rows
        switch (value)
        {
            case Dictionary<string, object?> dict:
                // Block format with rows array
                if (!dict.TryGetValue("rows", out var rowsObj) || rowsObj is not List<object?> rows)
                    throw new Exception("expected table with rows array");
                ValidateRows(rows);
                break;
            
            case List<object?> list:
                // Direct array of rows
                ValidateRows(list);
                break;
            
            default:
                throw new Exception($"expected table, got {value.GetType().Name}");
        }

        var refinementError = RunRefinements(value);
        if (refinementError != null)
            throw refinementError;
    }

    private void ValidateRows(List<object?> rows)
    {
        var errors = new List<ValidationError>();
        for (int i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            if (row is not Dictionary<string, object?> rowMap)
            {
                errors.Add(new ValidationError($"row[{i}]", "expected row object", row));
                continue;
            }

            try
            {
                _rowSchema.Validate(rowMap);
            }
            catch (ValidationErrors ve)
            {
                foreach (var e in ve.Errors)
                    errors.Add(new ValidationError($"row[{i}].{e.Field}", e.Message, e.Value));
            }
            catch (Exception ex)
            {
                errors.Add(new ValidationError($"row[{i}]", ex.Message, row));
            }
        }

        if (errors.Count > 0)
            throw new ValidationErrors(errors);
    }
}
