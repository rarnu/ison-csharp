using System.Text;

namespace Ison
{
    public class Parser
    {
        private readonly string[] _lines;
        private int _pos;

        public Parser(string text)
        {
            _lines = SplitLines(text);
            _pos = 0;
        }

        private static string[] SplitLines(string text)
        {
            return text.Split('\n')
                .Select(line => line.TrimEnd('\r'))
                .ToArray();
        }

        public Document Parse()
        {
            var doc = new Document();

            while (_pos < _lines.Length)
            {
                var line = _lines[_pos].Trim();

                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    _pos++;
                    continue;
                }

                if (line.Contains(".") && !line.StartsWith("\""))
                {
                    var parts = line.Split(new[] { '.' }, 2);
                    if (parts.Length == 2 && IsValidKind(parts[0]))
                    {
                        var block = ParseBlock(parts[0], parts[1]);
                        doc.AddBlock(block);
                        continue;
                    }
                }

                _pos++;
            }

            return doc;
        }

        private static bool IsValidKind(string kind)
        {
            return kind is "table" or "object" or "meta";
        }

        private Block ParseBlock(string kind, string name)
        {
            var block = new Block(kind, name);
            _pos++;

            while (_pos < _lines.Length)
            {
                var line = _lines[_pos].Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                {
                    _pos++;
                    continue;
                }
                break;
            }

            if (_pos >= _lines.Length)
            {
                return block;
            }

            var fieldsLine = _lines[_pos].Trim();
            var fields = TokenizeLine(fieldsLine);
            foreach (var field in fields)
            {
                var (fieldName, typeHint) = ParseFieldDef(field);
                block.AddField(fieldName, typeHint);
            }
            _pos++;

            var inSummary = false;
            while (_pos < _lines.Length)
            {
                var line = _lines[_pos].Trim();

                if (string.IsNullOrEmpty(line))
                {
                    _pos++;
                    break;
                }

                if (line.StartsWith("#"))
                {
                    _pos++;
                    continue;
                }

                if (line.Contains(".") && !line.StartsWith("\""))
                {
                    var parts = line.Split(new[] { '.' }, 2);
                    if (parts.Length == 2 && IsValidKind(parts[0]))
                    {
                        break;
                    }
                }

                if (line == "---")
                {
                    inSummary = true;
                    _pos++;
                    continue;
                }

                var tokens = TokenizeLine(line);
                var row = new Row();
                for (int i = 0; i < tokens.Count; i++)
                {
                    if (i < block.Fields.Count)
                    {
                        var fieldInfo = block.Fields[i];
                        var value = ParseValue(tokens[i], fieldInfo.TypeHint);
                        row[fieldInfo.Name] = value;
                    }
                }

                if (inSummary)
                {
                    block.SummaryRow = row;
                }
                else
                {
                    block.AddRow(row);
                }
                _pos++;
            }

            return block;
        }

        private static (string name, string typeHint) ParseFieldDef(string field)
        {
            var idx = field.IndexOf(':');
            if (idx > 0)
            {
                return (field[..idx], field[(idx + 1)..]);
            }
            return (field, "");
        }

        public static List<string> TokenizeLine(string line)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;
            var escaped = false;

            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];

                if (escaped)
                {
                    switch (ch)
                    {
                        case 'n':
                            current.Append('\n');
                            break;
                        case 't':
                            current.Append('\t');
                            break;
                        case '"':
                            current.Append('"');
                            break;
                        case '\\':
                            current.Append('\\');
                            break;
                        default:
                            current.Append(ch);
                            break;
                    }
                    escaped = false;
                    continue;
                }

                if (ch == '\\' && inQuotes)
                {
                    escaped = true;
                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }

                if (!inQuotes && (ch == ' ' || ch == '\t'))
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(ch);
            }

            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
            }

            return tokens;
        }

        public static Value ParseValue(string token, string typeHint)
        {
            if (token == "~" || token == "null" || token == "NULL")
            {
                return Value.Null();
            }

            if (token == "true" || token == "TRUE")
            {
                return Value.Bool(true);
            }
            if (token == "false" || token == "FALSE")
            {
                return Value.Bool(false);
            }

            if (token.StartsWith(":"))
            {
                var reference = ParseReference(token);
                return Value.Ref(reference);
            }

            switch (typeHint)
            {
                case "int":
                    if (long.TryParse(token, out var intVal))
                    {
                        return Value.Int(intVal);
                    }
                    break;
                case "float":
                    if (double.TryParse(token, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var floatVal))
                    {
                        return Value.Float(floatVal);
                    }
                    break;
                case "bool":
                    if (token == "true" || token == "1")
                    {
                        return Value.Bool(true);
                    }
                    if (token == "false" || token == "0")
                    {
                        return Value.Bool(false);
                    }
                    break;
                case "string":
                    return Value.String(token);
                case "ref":
                    if (token.StartsWith(":"))
                    {
                        return Value.Ref(ParseReference(token));
                    }
                    return Value.String(token);
            }

            if (long.TryParse(token, out var intValue))
            {
                return Value.Int(intValue);
            }

            if (double.TryParse(token, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var floatValue))
            {
                return Value.Float(floatValue);
            }

            return Value.String(token);
        }

        public static Reference ParseReference(string token)
        {
            if (!token.StartsWith(":"))
            {
                return new Reference(token);
            }

            token = token[1..];
            var parts = token.Split(new[] { ':' }, 2);

            if (parts.Length == 1)
            {
                return new Reference(parts[0]);
            }

            var ns = parts[0];
            var id = parts[1];

            if (ns.Length > 0 && ns.All(c => c == '_' || (c >= 'A' && c <= 'Z')))
            {
                return new Reference(id, relationship: ns);
            }

            return new Reference(id, ns);
        }
    }
}
