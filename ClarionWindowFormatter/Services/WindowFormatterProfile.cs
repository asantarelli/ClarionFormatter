using System.Collections.Generic;

namespace ClarionWindowFormatter.Services
{
    public class WindowFormatterProfile
    {
        public string ProfileName         { get; set; } = "Valores por defecto";
        public string AiProtocolFile      { get; set; } = "";
        public string AiExtraInstructions { get; set; } = "";
    }

    public class WindowFormatterSettings
    {
        public string AnthropicApiKey { get; set; } = "";
        public string AiModel         { get; set; } = "claude-sonnet-4-6";
        public string ActiveProfile   { get; set; } = "Valores por defecto";
        public List<WindowFormatterProfile> Profiles { get; set; } = new List<WindowFormatterProfile>
        {
            new WindowFormatterProfile()
        };
    }
}
