namespace Ison
{
    public struct DumpsOptions
    {
        public bool AlignColumns { get; set; }
        public string Delimiter { get; set; }

        public static DumpsOptions Default => new()
        {
            AlignColumns = false,
            Delimiter = " "
        };
    }

    public struct FromDictOptions
    {
        public bool AutoRefs { get; set; }
        public bool SmartOrder { get; set; }

        public static FromDictOptions Default => new()
        {
            AutoRefs = false,
            SmartOrder = false
        };
    }
}
