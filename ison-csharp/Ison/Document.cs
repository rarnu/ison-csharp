using System.Text.Json;

namespace Ison
{
    public class Document
    {
        public Dictionary<string, Block> Blocks { get; set; }
        public List<string> Order { get; set; }

        public Document()
        {
            Blocks = new Dictionary<string, Block>();
            Order = new List<string>();
        }

        public void AddBlock(Block block)
        {
            if (!Blocks.ContainsKey(block.Name))
            {
                Order.Add(block.Name);
            }
            Blocks[block.Name] = block;
        }

        public (Block? block, bool ok) Get(string name)
        {
            if (Blocks.TryGetValue(name, out var block))
            {
                return (block, true);
            }
            return (null, false);
        }

        public Dictionary<string, object> ToDict()
        {
            return Blocks.ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value.ToDict());
        }

        public string ToJson()
        {
            var result = new Dictionary<string, List<Dictionary<string, object?>>>();
            
            foreach (var kvp in Blocks)
            {
                var rows = kvp.Value.Rows.Select(row => 
                    row.ToDictionary(rkvp => rkvp.Key, rkvp => rkvp.Value.ToObject())
                ).ToList();
                result[kvp.Key] = rows;
            }

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}
