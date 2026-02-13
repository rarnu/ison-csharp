namespace Ison
{
    public class Block
    {
        public string Kind { get; set; }
        public string Name { get; set; }
        public List<FieldInfo> Fields { get; set; }
        public List<Row> Rows { get; set; }
        public Row? SummaryRow { get; set; }

        public Block(string kind, string name)
        {
            Kind = kind;
            Name = name;
            Fields = new List<FieldInfo>();
            Rows = new List<Row>();
        }

        public void AddField(string name, string typeHint = "")
        {
            Fields.Add(new FieldInfo(name, typeHint));
        }

        public void AddRow(Row row)
        {
            Rows.Add(row);
        }

        public List<string> GetFieldNames()
        {
            return Fields.Select(f => f.Name).ToList();
        }

        public Dictionary<string, object> ToDict()
        {
            var result = new Dictionary<string, object>
            {
                ["kind"] = Kind,
                ["name"] = Name,
                ["fields"] = Fields.Select(f => new Dictionary<string, object>
                {
                    ["name"] = f.Name,
                    ["typeHint"] = f.TypeHint
                }).ToList(),
                ["rows"] = Rows.Select(row =>
                    row.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToObject())
                ).ToList()
            };

            if (SummaryRow != null)
            {
                result["summary"] = SummaryRow.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToObject());
            }

            return result;
        }
    }
}