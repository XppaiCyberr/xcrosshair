using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace xcrosshair
{
    public class ValorantProfile
    {
        public class Section
        {
            public int ColorIndex { get; set; } = 1; // Default Green
            public string CustomColor { get; set; } = "";
            public bool CustomColorEnabled { get; set; } = false;
            
            public bool OutlinesEnabled { get; set; } = true;
            public double OutlineOpacity { get; set; } = 0.5;
            public int OutlineThickness { get; set; } = 1;

            public bool CenterDotEnabled { get; set; } = false;
            public double CenterDotOpacity { get; set; } = 1.0;
            public int CenterDotThickness { get; set; } = 2;

            public bool FadeTopLines { get; set; } = false;
            public bool ShowSpectatorCrosshair { get; set; } = true;
            public bool OverrideFiringError { get; set; } = false;

            public LineSettings InnerLines { get; set; } = new LineSettings { Show = true, Opacity = 0.8, Thickness = 2, Length = 6, Offset = 3 };
            public LineSettings OuterLines { get; set; } = new LineSettings { Show = false, Opacity = 0.35, Thickness = 2, Length = 2, Offset = 10 };

            public Dictionary<string, string> UnknownTokens { get; set; } = new Dictionary<string, string>();
        }

        public class LineSettings
        {
            public bool Show { get; set; } = true;
            public double Opacity { get; set; } = 1.0;
            public int Thickness { get; set; } = 2;
            public int Length { get; set; } = 5;
            public int VerticalLength { get; set; } = 5;
            public bool IndependentLength { get; set; } = false;
            public int Offset { get; set; } = 3;
            public bool MovementErrorEnabled { get; set; } = false;
            public double MovementErrorMultiplier { get; set; } = 1.0;
            public bool FiringErrorEnabled { get; set; } = false;
            public double FiringErrorMultiplier { get; set; } = 1.0;
        }

        public Section Primary { get; set; } = new Section();
        public Section? ADS { get; set; }
        public Section? Sniper { get; set; }

        public static ValorantProfile Parse(string code)
        {
            var profile = new ValorantProfile();
            if (string.IsNullOrWhiteSpace(code)) return profile;

            var tokens = code.Split(';');
            if (tokens.Length == 0 || tokens[0] != "0") return profile;

            Section? currentSection = null;

            for (int i = 1; i < tokens.Length; i++)
            {
                var token = tokens[i];
                if (string.IsNullOrEmpty(token)) continue;

                if (token == "P") { profile.Primary = new Section(); currentSection = profile.Primary; continue; }
                if (token == "A") { profile.ADS = new Section(); currentSection = profile.ADS; continue; }
                if (token == "S") { profile.Sniper = new Section(); currentSection = profile.Sniper; continue; }

                if (currentSection == null) continue;

                // Value is usually the next token
                if (i + 1 >= tokens.Length) break;
                var value = tokens[++i];

                try
                {
                    switch (token)
                    {
                        case "c": currentSection.ColorIndex = int.Parse(value); break;
                        case "u": currentSection.CustomColor = value; break;
                        case "b": currentSection.CustomColorEnabled = value == "1"; break;
                        case "h": currentSection.OutlinesEnabled = value == "1"; break;
                        case "o": currentSection.OutlineOpacity = ParseDouble(value); break;
                        case "t": currentSection.OutlineThickness = int.Parse(value); break;
                        case "d": currentSection.CenterDotEnabled = value == "1"; break;
                        case "a": currentSection.CenterDotOpacity = ParseDouble(value); break;
                        case "z": currentSection.CenterDotThickness = int.Parse(value); break;
                        case "f": currentSection.FadeTopLines = value == "1"; break;
                        case "s": currentSection.ShowSpectatorCrosshair = value == "1"; break;
                        case "m": currentSection.OverrideFiringError = value == "1"; break;

                        // Inner Lines
                        case "0b": currentSection.InnerLines.Show = value == "1"; break;
                        case "0a": currentSection.InnerLines.Opacity = ParseDouble(value); break;
                        case "0t": currentSection.InnerLines.Thickness = int.Parse(value); break;
                        case "0l": currentSection.InnerLines.Length = int.Parse(value); break;
                        case "0v": currentSection.InnerLines.VerticalLength = int.Parse(value); break;
                        case "0g": currentSection.InnerLines.IndependentLength = value == "1"; break;
                        case "0o": currentSection.InnerLines.Offset = int.Parse(value); break;
                        case "0m": currentSection.InnerLines.MovementErrorEnabled = value == "1"; break;
                        case "0s": currentSection.InnerLines.MovementErrorMultiplier = ParseDouble(value); break;
                        case "0f": currentSection.InnerLines.FiringErrorEnabled = value == "1"; break;
                        case "0e": currentSection.InnerLines.FiringErrorMultiplier = ParseDouble(value); break;

                        // Outer Lines
                        case "1b": currentSection.OuterLines.Show = value == "1"; break;
                        case "1a": currentSection.OuterLines.Opacity = ParseDouble(value); break;
                        case "1t": currentSection.OuterLines.Thickness = int.Parse(value); break;
                        case "1l": currentSection.OuterLines.Length = int.Parse(value); break;
                        case "1v": currentSection.OuterLines.VerticalLength = int.Parse(value); break;
                        case "1g": currentSection.OuterLines.IndependentLength = value == "1"; break;
                        case "1o": currentSection.OuterLines.Offset = int.Parse(value); break;
                        case "1m": currentSection.OuterLines.MovementErrorEnabled = value == "1"; break;
                        case "1s": currentSection.OuterLines.MovementErrorMultiplier = ParseDouble(value); break;
                        case "1f": currentSection.OuterLines.FiringErrorEnabled = value == "1"; break;
                        case "1e": currentSection.OuterLines.FiringErrorMultiplier = ParseDouble(value); break;

                        default:
                            currentSection.UnknownTokens[token] = value;
                            break;
                    }
                }
                catch { }
            }

            return profile;
        }

        private static double ParseDouble(string value)
        {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                return result;
            return 0;
        }

        public string ToCode()
        {
            var sb = new StringBuilder("0");
            AppendSection(sb, "P", Primary);
            if (ADS != null) AppendSection(sb, "A", ADS);
            if (Sniper != null) AppendSection(sb, "S", Sniper);
            return sb.ToString();
        }

        private void AppendSection(StringBuilder sb, string prefix, Section s)
        {
            sb.Append($";{prefix}");
            if (s.ColorIndex != 1) sb.Append($";c;{s.ColorIndex}");
            if (!string.IsNullOrEmpty(s.CustomColor)) sb.Append($";u;{s.CustomColor}");
            if (s.CustomColorEnabled) sb.Append($";b;1");
            if (!s.OutlinesEnabled) sb.Append($";h;0");
            if (s.OutlineOpacity != 0.5) sb.Append($";o;{FormatDouble(s.OutlineOpacity)}");
            if (s.OutlineThickness != 1) sb.Append($";t;{s.OutlineThickness}");
            if (s.CenterDotEnabled) sb.Append($";d;1");
            if (s.CenterDotOpacity != 1.0) sb.Append($";a;{FormatDouble(s.CenterDotOpacity)}");
            if (s.CenterDotThickness != 2) sb.Append($";z;{s.CenterDotThickness}");
            if (s.FadeTopLines) sb.Append($";f;1");
            if (!s.ShowSpectatorCrosshair) sb.Append($";s;0");
            if (s.OverrideFiringError) sb.Append($";m;1");

            AppendLineSettings(sb, "0", s.InnerLines);
            AppendLineSettings(sb, "1", s.OuterLines);

            foreach (var kvp in s.UnknownTokens)
            {
                sb.Append($";{kvp.Key};{kvp.Value}");
            }
        }

        private void AppendLineSettings(StringBuilder sb, string prefix, LineSettings l)
        {
            if (prefix == "0" && !l.Show) sb.Append($";0b;0");
            if (prefix == "1" && l.Show) sb.Append($";1b;1");

            if (l.Opacity != 1.0) sb.Append($";{prefix}a;{FormatDouble(l.Opacity)}");
            if (l.Thickness != 2) sb.Append($";{prefix}t;{l.Thickness}");
            if (l.Length != 5) sb.Append($";{prefix}l;{l.Length}");
            if (l.VerticalLength != 5 && l.IndependentLength) sb.Append($";{prefix}v;{l.VerticalLength}");
            if (l.IndependentLength) sb.Append($";{prefix}g;1");
            if (l.Offset != 3) sb.Append($";{prefix}o;{l.Offset}");
            if (l.MovementErrorEnabled) sb.Append($";{prefix}m;1");
            if (l.MovementErrorMultiplier != 1.0) sb.Append($";{prefix}s;{FormatDouble(l.MovementErrorMultiplier)}");
            if (l.FiringErrorEnabled) sb.Append($";{prefix}f;1");
            if (l.FiringErrorMultiplier != 1.0) sb.Append($";{prefix}e;{FormatDouble(l.FiringErrorMultiplier)}");
        }

        private string FormatDouble(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public string GetColorHex()
        {
            if (Primary.ColorIndex == 8 && !string.IsNullOrEmpty(Primary.CustomColor))
                return "#" + Primary.CustomColor;

            return Primary.ColorIndex switch
            {
                0 => "#FFFFFF", // White
                1 => "#00FF00", // Green
                2 => "#7FFF00", // Yellow Green
                3 => "#ADFF2F", // Green Yellow
                4 => "#FFFF00", // Yellow
                5 => "#00FFFF", // Cyan
                6 => "#FFC0CB", // Pink
                7 => "#FF0000", // Red
                _ => "#00FF00"  // Default Green
            };
        }
    }
}
