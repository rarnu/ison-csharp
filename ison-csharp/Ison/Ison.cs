using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace Ison
{
    public static class Ison
    {
        public static Document Parse(string text)
        {
            var parser = new Parser(text);
            return parser.Parse();
        }

        public static Document Load(string path)
        {
            var text = File.ReadAllText(path);
            return Parse(text);
        }

        public static string Dumps(Document doc)
        {
            return DumpsWithOptions(doc, DumpsOptions.Default);
        }

        public static string DumpsWithOptions(Document doc, DumpsOptions opts)
        {
            var sb = new StringBuilder();
            var delim = string.IsNullOrEmpty(opts.Delimiter) ? " " : opts.Delimiter;

            for (int i = 0; i < doc.Order.Count; i++)
            {
                if (i > 0)
                {
                    sb.AppendLine();
                }

                var block = doc.Blocks[doc.Order[i]];
                sb.AppendLine($"{block.Kind}.{block.Name}");

                for (int j = 0; j < block.Fields.Count; j++)
                {
                    if (j > 0)
                    {
                        sb.Append(delim);
                    }
                    var field = block.Fields[j];
                    if (!string.IsNullOrEmpty(field.TypeHint))
                    {
                        sb.Append($"{field.Name}:{field.TypeHint}");
                    }
                    else
                    {
                        sb.Append(field.Name);
                    }
                }
                sb.AppendLine();

                foreach (var row in block.Rows)
                {
                    for (int j = 0; j < block.Fields.Count; j++)
                    {
                        if (j > 0)
                        {
                            sb.Append(delim);
                        }
                        var field = block.Fields[j];
                        if (row.TryGetValue(field.Name, out var val))
                        {
                            sb.Append(val.ToIson());
                        }
                        else
                        {
                            sb.Append("~");
                        }
                    }
                    sb.AppendLine();
                }

                if (block.SummaryRow != null)
                {
                    sb.AppendLine("---");
                    for (int j = 0; j < block.Fields.Count; j++)
                    {
                        if (j > 0)
                        {
                            sb.Append(delim);
                        }
                        var field = block.Fields[j];
                        if (block.SummaryRow.TryGetValue(field.Name, out var val))
                        {
                            sb.Append(val.ToIson());
                        }
                        else
                        {
                            sb.Append("~");
                        }
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public static void Dump(Document doc, string path)
        {
            var text = Dumps(doc);
            File.WriteAllText(path, text);
        }

        public static void DumpWithOptions(Document doc, string path, DumpsOptions opts)
        {
            var text = DumpsWithOptions(doc, opts);
            File.WriteAllText(path, text);
        }

        public static string DumpsIsonl(Document doc)
        {
            var sb = new StringBuilder();

            foreach (var name in doc.Order)
            {
                var block = doc.Blocks[name];

                var fieldHeader = new StringBuilder();
                for (int i = 0; i < block.Fields.Count; i++)
                {
                    if (i > 0)
                    {
                        fieldHeader.Append(' ');
                    }
                    var field = block.Fields[i];
                    if (!string.IsNullOrEmpty(field.TypeHint))
                    {
                        fieldHeader.Append($"{field.Name}:{field.TypeHint}");
                    }
                    else
                    {
                        fieldHeader.Append(field.Name);
                    }
                }
                var fields = fieldHeader.ToString();

                foreach (var row in block.Rows)
                {
                    sb.Append($"{block.Kind}.{block.Name}|{fields}|");
                    for (int i = 0; i < block.Fields.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(' ');
                        }
                        var field = block.Fields[i];
                        if (row.TryGetValue(field.Name, out var val))
                        {
                            sb.Append(val.ToIson());
                        }
                        else
                        {
                            sb.Append('~');
                        }
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }

        public static Document ParseIsonl(string text)
        {
            var doc = new Document();
            var lines = text.Split('\n').Select(l => l.TrimEnd('\r')).ToArray();

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                var parts = trimmed.Split(['|'], 3);
                if (parts.Length != 3)
                {
                    continue;
                }

                var header = parts[0];
                var headerParts = header.Split(['.'], 2);
                if (headerParts.Length != 2)
                {
                    continue;
                }
                var kind = headerParts[0];
                var name = headerParts[1];

                var (block, exists) = doc.Get(name);
                if (!exists)
                {
                    block = new Block(kind, name);
                    doc.AddBlock(block);

                    var fieldTokens = Parser.TokenizeLine(parts[1]);
                    foreach (var field in fieldTokens)
                    {
                        var (fname, ftype) = ParseFieldDef(field);
                        block.AddField(fname, ftype);
                    }
                }

                Debug.Assert(block != null, nameof(block) + " != null");

#pragma warning disable CS8602 // Dereference of a possibly null reference.
                var tokens = Parser.TokenizeLine(parts[2]);
                var row = new Row();
                for (int i = 0; i < tokens.Count; i++)
                {
                    if (i < block.Fields.Count)
                    {
                        var fieldInfo = block.Fields[i];
                        var value = Parser.ParseValue(tokens[i], fieldInfo.TypeHint);
                        row[fieldInfo.Name] = value;
                    }
                }
                block.AddRow(row);
            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            return doc;
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

        public static Document LoadIsonl(string path)
        {
            var text = File.ReadAllText(path);
            return ParseIsonl(text);
        }

        public static void DumpIsonl(Document doc, string path)
        {
            var text = DumpsIsonl(doc);
            File.WriteAllText(path, text);
        }

        public static string IsonToIsonl(string isonText)
        {
            var doc = Parse(isonText);
            return DumpsIsonl(doc);
        }

        public static string IsonlToIson(string isonlText)
        {
            var doc = ParseIsonl(isonlText);
            return Dumps(doc);
        }

        public class IsonlRecord
        {
            public string Kind { get; set; } = "";
            public string Name { get; set; } = "";
            public List<string> Fields { get; set; } = [];
            public Dictionary<string, Value> Values { get; set; } = [];
        }

        public static ChannelReader<IsonlRecord> IsonlStream(TextReader reader)
        {
            var channel = Channel.CreateUnbounded<IsonlRecord>();

            _ = Task.Run(async () =>
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    {
                        continue;
                    }

                    var parts = trimmed.Split(['|'], 3);
                    if (parts.Length != 3)
                    {
                        continue;
                    }

                    var header = parts[0];
                    var headerParts = header.Split(['.'], 2);
                    if (headerParts.Length != 2)
                    {
                        continue;
                    }

                    var fieldTokens = TokenizeLine(parts[1]);
                    var fields = new List<string>();
                    var fieldTypes = new List<string>();
                    foreach (var field in fieldTokens)
                    {
                        var (fname, ftype) = ParseFieldDef(field);
                        fields.Add(fname);
                        fieldTypes.Add(ftype);
                    }

                    var tokens = TokenizeLine(parts[2]);
                    var values = new Dictionary<string, Value>();
                    for (int i = 0; i < tokens.Count; i++)
                    {
                        if (i < fields.Count)
                        {
                            var value = Parser.ParseValue(tokens[i], fieldTypes[i]);
                            values[fields[i]] = value;
                        }
                    }

                    await channel.Writer.WriteAsync(new IsonlRecord
                    {
                        Kind = headerParts[0],
                        Name = headerParts[1],
                        Fields = fields,
                        Values = values
                    });
                }

                channel.Writer.Complete();
            });

            return channel.Reader;
        }

        private static List<string> TokenizeLine(string line)
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

        public static string ToJson(string isonText)
        {
            var doc = Parse(isonText);
            return doc.ToJson();
        }

        public static Document FromJson(string jsonText)
        {
            using var doc = JsonDocument.Parse(jsonText);
            var result = new Document();

            foreach (var property in doc.RootElement.EnumerateObject())
            {
                var name = property.Name;
                var value = property.Value;

                switch (value.ValueKind)
                {
                    case JsonValueKind.Array:
                        var block = new Block("table", name);

                        var elements = value.EnumerateArray().ToList();
                        if (elements.Count > 0 && elements[0].ValueKind == JsonValueKind.Object)
                        {
                            foreach (var key in elements[0].EnumerateObject().Select(p => p.Name))
                            {
                                block.AddField(key);
                            }
                        }

                        foreach (var item in elements)
                        {
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                var row = new Row();
                                foreach (var prop in item.EnumerateObject())
                                {
                                    row[prop.Name] = JsonElementToValue(prop.Value);
                                }
                                block.AddRow(row);
                            }
                        }

                        result.AddBlock(block);
                        break;

                    case JsonValueKind.Object:
                        var objBlock = new Block("object", name);
                        foreach (var key in value.EnumerateObject().Select(p => p.Name))
                        {
                            objBlock.AddField(key);
                        }
                        var objRow = new Row();
                        foreach (var prop in value.EnumerateObject())
                        {
                            objRow[prop.Name] = JsonElementToValue(prop.Value);
                        }
                        objBlock.AddRow(objRow);
                        result.AddBlock(objBlock);
                        break;
                }
            }

            return result;
        }

        private static Value JsonElementToValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => Value.Null(),
                JsonValueKind.True => Value.Bool(true),
                JsonValueKind.False => Value.Bool(false),
                JsonValueKind.Number => element.TryGetInt64(out var intVal)
                    ? Value.Int(intVal)
                    : Value.Float(element.GetDouble()),
                JsonValueKind.String => Value.String(element.GetString() ?? ""),
                _ => Value.String(element.ToString())
            };
        }

        public static Document FromDict(Dictionary<string, object> data)
        {
            return FromDictWithOptions(data, FromDictOptions.Default);
        }

        public static Document FromDictWithOptions(Dictionary<string, object> data, FromDictOptions opts)
        {
            var doc = new Document();

            var tableNames = new HashSet<string>(data.Keys);

            var refFields = new Dictionary<string, string>();
            if (opts.AutoRefs)
            {
                foreach (var kvp in data)
                {
                    var tableName = kvp.Key;
                    var tableData = kvp.Value;

                    if (tableData is List<object> arr && arr.Count > 0 && arr[0] is Dictionary<string, object> firstRow)
                    {
                        foreach (var key in firstRow.Keys)
                        {
                            if (key.EndsWith("_id") && key != "id")
                            {
                                var refType = key[..^3];
                                if (tableNames.Contains(refType + "s") || tableNames.Contains(refType))
                                {
                                    refFields[key] = refType;
                                }
                            }
                        }
                    }

                    if (tableName == "edges" && tableNames.Contains("nodes"))
                    {
                        refFields["source"] = "node";
                        refFields["target"] = "node";
                    }
                }
            }

            var names = data.Keys.OrderBy(n => n).ToList();

            foreach (var name in names)
            {
                var content = data[name];

                switch (content)
                {
                    case List<object> list:
                        if (list.Count > 0 && list[0] is Dictionary<string, object>)
                        {
                            var fieldSet = new HashSet<string>();
                            var fieldOrder = new List<string>();
                            foreach (var item in list)
                            {
                                if (item is Dictionary<string, object> row)
                                {
                                    foreach (var key in row.Keys)
                                    {
                                        if (fieldSet.Add(key))
                                        {
                                            fieldOrder.Add(key);
                                        }
                                    }
                                }
                            }

                            if (opts.SmartOrder)
                            {
                                fieldOrder = SmartOrderFields(fieldOrder);
                            }

                            var block = new Block("table", name);
                            foreach (var field in fieldOrder)
                            {
                                block.AddField(field);
                            }

                            foreach (var item in list)
                            {
                                if (item is Dictionary<string, object> rowData)
                                {
                                    var row = new Row();
                                    foreach (var kvp in rowData)
                                    {
                                        var key = kvp.Key;
                                        var val = kvp.Value;

                                        if (opts.AutoRefs && refFields.TryGetValue(key, out var refType))
                                        {
                                            row[key] = Value.Ref(new Reference(val.ToString() ?? "", ns: refType));
                                            continue;
                                        }
                                        row[key] = ObjectToValue(val);
                                    }
                                    block.AddRow(row);
                                }
                            }

                            doc.AddBlock(block);
                        }
                        break;

                    case Dictionary<string, object> dict:
                        var objBlock = new Block("object", name);
                        var fields = dict.Keys.ToList();
                        if (opts.SmartOrder)
                        {
                            fields = SmartOrderFields(fields);
                        }
                        foreach (var key in fields)
                        {
                            objBlock.AddField(key);
                        }
                        var objRow = new Row();
                        foreach (var kvp in dict)
                        {
                            objRow[kvp.Key] = ObjectToValue(kvp.Value);
                        }
                        objBlock.AddRow(objRow);
                        doc.AddBlock(objBlock);
                        break;
                }
            }

            return doc;
        }

        private static List<string> SmartOrderFields(List<string> fields)
        {
            var priorityNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "name", "title", "label", "description", "display_name", "full_name"
            };

            var idFields = new List<string>();
            var nameFields = new List<string>();
            var refFields = new List<string>();
            var otherFields = new List<string>();

            foreach (var field in fields)
            {
                var fieldLower = field.ToLowerInvariant();
                if (fieldLower == "id")
                {
                    idFields.Add(field);
                }
                else if (priorityNames.Contains(fieldLower))
                {
                    nameFields.Add(field);
                }
                else if (fieldLower.EndsWith("_id") && fieldLower != "id")
                {
                    refFields.Add(field);
                }
                else
                {
                    otherFields.Add(field);
                }
            }

            var result = new List<string>();
            result.AddRange(idFields);
            result.AddRange(nameFields);
            result.AddRange(otherFields);
            result.AddRange(refFields);
            return result;
        }

        private static Value ObjectToValue(object? obj)
        {
            return obj switch
            {
                null => Value.Null(),
                bool b => Value.Bool(b),
                int i => Value.Int(i),
                long l => Value.Int(l),
                float f => Value.Float(f),
                double d => Value.Float(d),
                string s => Value.String(s),
                _ => Value.String(obj.ToString() ?? "")
            };
        }
    }
}
