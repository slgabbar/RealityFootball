namespace RealityFootball.Api.Options;

public class YahooOptions
{
    public const string SectionName = "Yahoo";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
