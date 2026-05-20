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
            
            public bool OutlinesEnabled { get; set; } = true;
            public double OutlineOpacity { get; set; } = 0.5;
            public int OutlineThickness { get; set; } = 1;

            public bool CenterDotEnabled { get; set; } = false;
            public double CenterDotOpacity { get; set; } = 1.0;
            public int CenterDotThickness { get; set; } = 2;

            public double MovementErrorMultiplier { get; set; } = 1.0;
            public double FiringErrorMultiplier { get; set; } = 1.0;
            public bool ShowSpectatorCrosshair { get; set; } = true;
            public bool OverrideMapSettings { get; set; } = false;

            public LineSettings InnerLines { get; set; } = new LineSettings { Show = true, Opacity = 0.8, Thickness = 2, Length = 4, Offset = 3 };
            public LineSettings OuterLines { get; set; } = new LineSettings { Show = false, Opacity = 0.35, Thickness = 2, Length = 2, Offset = 10 };

            public Dictionary<string, string> UnknownTokens { get; set; } = new Dictionary<string, string>();
        }

        public class LineSettings
        {
            public bool Show { get; set; } = true;
            public double Opacity { get; set; } = 1.0;
            public int Thickness { get; set; } = 2;
            public int Length { get; set; } = 5;
            public int Offset { get; set; } = 3;
            public bool MovementErrorEnabled { get; set; } = false;
            public bool FiringErrorEnabled { get; set; } = false;
            public double ErrorMultiplier { get; set; } = 1.0;
            public int AdsOffset { get; set; } = 0;
        }

        public Section Primary { get; set; } = new Section();
        public Section? ADS { get; set; }
        public Section? Sniper { get; set; }

        public static ValorantProfile Parse(string code)
        {
            var profile = new ValorantProfile();
            if (string.IsNullOrWhiteSpace(code)) return profile;

            var tokens = code.Split(';');
            if (tokens.Length == 0 || (tokens[0] != "0" && tokens[0] != "1")) return profile;

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
                        case "h": currentSection.CenterDotEnabled = value == "1"; break;
                        case "t": currentSection.CenterDotThickness = int.Parse(value); break;
                        case "d": currentSection.CenterDotOpacity = ParseDouble(value); break;
                        case "z": currentSection.OverrideMapSettings = value == "1"; break;
                        case "m": currentSection.MovementErrorMultiplier = ParseDouble(value); break;
                        case "f": currentSection.FiringErrorMultiplier = ParseDouble(value); break;
                        case "s": currentSection.ShowSpectatorCrosshair = value == "1"; break;
                        case "b": currentSection.OutlinesEnabled = value == "0"; break; // b;1 means disable outlines
                        case "o": currentSection.OutlinesEnabled = value == "1"; break; // o;1 means show outline
                        case "ot": currentSection.OutlineThickness = int.Parse(value); break;
                        case "oa": currentSection.OutlineOpacity = ParseDouble(value); break;

                        // Inner Lines (0)
                        case "0l": currentSection.InnerLines.Length = int.Parse(value); break;
                        case "0v": currentSection.InnerLines.Thickness = int.Parse(value); break;
                        case "0o": currentSection.InnerLines.Offset = int.Parse(value); break;
                        case "0a": currentSection.InnerLines.Opacity = ParseDouble(value); break;
                        case "0g": currentSection.InnerLines.Show = value == "1"; break;
                        case "0e": currentSection.InnerLines.MovementErrorEnabled = value == "1"; break;
                        case "0f": currentSection.InnerLines.FiringErrorEnabled = value == "1"; break;
                        case "0m": currentSection.InnerLines.ErrorMultiplier = ParseDouble(value); break;
                        case "0s": currentSection.InnerLines.AdsOffset = int.Parse(value); break;

                        // Outer Lines (1)
                        case "1l": currentSection.OuterLines.Length = int.Parse(value); break;
                        case "1v": currentSection.OuterLines.Thickness = int.Parse(value); break;
                        case "1o": currentSection.OuterLines.Offset = int.Parse(value); break;
                        case "1a": currentSection.OuterLines.Opacity = ParseDouble(value); break;
                        case "1g": currentSection.OuterLines.Show = value == "1"; break;
                        case "1e": currentSection.OuterLines.MovementErrorEnabled = value == "1"; break;
                        case "1f": currentSection.OuterLines.FiringErrorEnabled = value == "1"; break;
                        case "1m": currentSection.OuterLines.ErrorMultiplier = ParseDouble(value); break;
                        case "1s": currentSection.OuterLines.AdsOffset = int.Parse(value); break;

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
            if (s.CenterDotEnabled) sb.Append($";h;1");
            if (s.CenterDotThickness != 2) sb.Append($";t;{s.CenterDotThickness}");
            if (s.CenterDotOpacity != 1.0) sb.Append($";d;{FormatDouble(s.CenterDotOpacity)}");
            if (s.OverrideMapSettings) sb.Append($";z;1");
            if (s.MovementErrorMultiplier != 1.0) sb.Append($";m;{FormatDouble(s.MovementErrorMultiplier)}");
            if (s.FiringErrorMultiplier != 1.0) sb.Append($";f;{FormatDouble(s.FiringErrorMultiplier)}");
            if (!s.ShowSpectatorCrosshair) sb.Append($";s;0");
            
            if (!s.OutlinesEnabled) sb.Append($";o;0");
            if (s.OutlineThickness != 1) sb.Append($";ot;{s.OutlineThickness}");
            if (s.OutlineOpacity != 0.5) sb.Append($";oa;{FormatDouble(s.OutlineOpacity)}");

            AppendLineSettings(sb, "0", s.InnerLines);
            AppendLineSettings(sb, "1", s.OuterLines);

            foreach (var kvp in s.UnknownTokens)
            {
                sb.Append($";{kvp.Key};{kvp.Value}");
            }
        }

        private void AppendLineSettings(StringBuilder sb, string prefix, LineSettings l)
        {
            if (prefix == "0" && !l.Show) sb.Append($";0g;0");
            if (prefix == "1" && l.Show) sb.Append($";1g;1");

            if (l.Length != 5) sb.Append($";{prefix}l;{l.Length}");
            if (l.Thickness != 2) sb.Append($";{prefix}v;{l.Thickness}");
            if (l.Offset != 3) sb.Append($";{prefix}o;{l.Offset}");
            if (l.Opacity != 1.0) sb.Append($";{prefix}a;{FormatDouble(l.Opacity)}");
            if (l.MovementErrorEnabled) sb.Append($";{prefix}e;1");
            if (l.FiringErrorEnabled) sb.Append($";{prefix}f;1");
            if (l.ErrorMultiplier != 1.0) sb.Append($";{prefix}m;{FormatDouble(l.ErrorMultiplier)}");
            if (l.AdsOffset != 0) sb.Append($";{prefix}s;{l.AdsOffset}");
        }

        private string FormatDouble(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        public string GetColorHex()
        {
            if (Primary.ColorIndex == 8 && !string.IsNullOrEmpty(Primary.CustomColor))
            {
                // Valorant uses RRGGBBAA. WPF uses #AARRGGBB.
                string hex = Primary.CustomColor;
                if (hex.Length == 8)
                {
                    string rr = hex.Substring(0, 2);
                    string gg = hex.Substring(2, 2);
                    string bb = hex.Substring(4, 2);
                    string aa = hex.Substring(6, 2);
                    return $"#{aa}{rr}{gg}{bb}";
                }
                return "#" + hex;
            }

            return Primary.ColorIndex switch
            {
                0 => "#FFFFFF", // White
                1 => "#00FF00", // Green
                2 => "#7FFF00", // Yellow Green
                3 => "#ADFF2F", // Green (brighter)
                4 => "#00FFFF", // Cyan
                5 => "#00CED1", // Cyan (bright)
                6 => "#FF00FF", // Pink / Magenta
                7 => "#FF0000", // Red
                _ => "#00FF00"  // Default Green
            };
        }
    }
}
