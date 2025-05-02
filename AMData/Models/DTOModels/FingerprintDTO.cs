namespace AMData.Models.DTOModels;

public class FingerprintDTO
{
    public string IPAddress { get; set; }
    public string UserAgent { get; set; }
    public string Platform { get; set; }
    public string Language { get; set; }

    public void Validate()
    {
        IPAddress = string.IsNullOrWhiteSpace(IPAddress) ? string.Empty : IPAddress;
        UserAgent = string.IsNullOrWhiteSpace(UserAgent) ? string.Empty : UserAgent;
        Platform = string.IsNullOrWhiteSpace(Platform) ? string.Empty : Platform;
        Language = string.IsNullOrWhiteSpace(Language) ? string.Empty : Language;
    }
}