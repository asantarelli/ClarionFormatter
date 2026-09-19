using System.Collections.Generic;

namespace ClarionWindowFormatter.Services
{
    public class ControlTypeRule
    {
        public string ControlType  { get; set; } = "";
        public string YBase        { get; set; } = "";
        public string YIncrement   { get; set; } = "";
        public string XLabel       { get; set; } = "";
        public string XControl     { get; set; } = "";
        public string Height       { get; set; } = "";
        public string MinWidth     { get; set; } = "";
        public string MaxWidth     { get; set; } = "";
        public string ColorAttr    { get; set; } = "";
        public bool   GenerateTip  { get; set; } = false;
        public string TipTemplate  { get; set; } = "";
        public string ExtraRules   { get; set; } = "";
    }

    public class WindowFormatterProfile
    {
        public string ProfileName         { get; set; } = "Valores por defecto";
        public string AiExtraInstructions { get; set; } = "";
        public List<ControlTypeRule> ControlRules { get; set; } = new List<ControlTypeRule>();
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
