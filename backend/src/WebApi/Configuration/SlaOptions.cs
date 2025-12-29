namespace WebApi.Configuration;

public class SlaOptions
{
    public int FirstResponseMinutes { get; set; } = 60;
    public int ResolutionMinutes { get; set; } = 480;
}
