namespace OysterSmiling.Models
{
    public record BotSettings
    {
        public string Token { get; init; }
        public string CommandPrefix { get; init; }
    }
}