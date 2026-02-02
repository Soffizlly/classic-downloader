namespace ClassicDownloader.Services
{
    public class MediaMetadata
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Year { get; set; }
        public string Genre { get; set; }
        public string Duration { get; set; }
    }

    public class MetadataSection
    {
        public string Title { get; set; }
        public System.Collections.Generic.List<MetadataItem> Items { get; set; }

        public MetadataSection()
        {
            Items = new System.Collections.Generic.List<MetadataItem>();
        }
    }

    public class MetadataItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
