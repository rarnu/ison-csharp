namespace Ison
{
    public class Row : Dictionary<string, Value>
    {
        public Row() : base() { }
        public Row(IDictionary<string, Value> dictionary) : base(dictionary) { }
    }
}
