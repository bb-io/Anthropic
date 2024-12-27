namespace Apps.Anthropic.Models.Response;

public class RequestCountResponse
{
    public int Processing { get; set; }
    
    public int Succeeded { get; set; }
    
    public int Errored { get; set; }
    
    public int Canceled { get; set; }
    
    public int Expired { get; set; }
}