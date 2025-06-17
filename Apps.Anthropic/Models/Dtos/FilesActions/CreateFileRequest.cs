namespace TestPlugin.Dtos.FilesActions;

public class CreateFileRequest
{
    public string DownloadUrl { get; set; }
        
    public string? Name { get; set; }
        
    public string? ContentType { get; set; }
}