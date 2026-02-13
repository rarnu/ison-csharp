namespace Ison
{
    public struct FieldInfo
    {
        public string Name { get; set; }
        public string TypeHint { get; set; }

        public FieldInfo(string name, string typeHint = "")
        {
            Name = name;
            TypeHint = typeHint;
        }
    }
}
