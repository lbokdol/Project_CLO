using System.Text.Json.Serialization;

namespace Project_CLO.Common
{
    public class DiskInformation
    {
        public int Uid { get; set; }
        public long Size { get; set; }
        [JsonIgnore]
        public string RootDirectoryPath { get; set; }
    }

    public class ContentsStructure
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public ContentType Type { get; set; }
        public List<ContentsStructure> Children { get; set; }
    }

    public class RequestContentInformation
    {
        public string Path { get; set; }
        public string Content { get; set; }
        public ContentType Type { get; set; }
        public bool IsOverwrite { get; set; }
    }

    public class ContentInformation: RequestContentInformation
    {
        public bool IsCreated { get; set; }
    }

    public class APIInformation
    {
        public string Path { get; set; }
        public int Count { get; set; }
        public MethodType MethodType { get; set; }
    }

    public enum ContentType
    {
        Folder,
        File,
    }

    public enum MethodType
    {
        GET,
        POST,
        PUT,
        DELETE,
    }
}
