using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;

namespace xcrosshair
{
    public partial class MainWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const int WS_EX_TOPMOST = 0x00000008;

        private const int HOTKEY_ID = 9000;
        private const uint WM_HOTKEY = 0x0312;
        private const uint VK_HOME = 0x24;

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOACTIVATE = 0x0010;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private IntPtr _hwnd;
        private bool _isLoaded = false;
        private CrosshairSettings _settings = new CrosshairSettings();
        private ValorantProfile? _valorantProfile = null;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LogError(string context, Exception ex)
        {
            try 
            {
                string logPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                System.IO.File.AppendAllText(logPath, $"[{DateTime.Now}] Error in {context}: {ex.Message}\n{ex.StackTrace}\n\n");
            } catch {}
        }

        private void ApplySettings()
        {
            try 
            {
                if (SizeSlider == null || ThicknessSlider == null || PositionComboBox == null) return;

                var profile = _settings.CurrentProfile;
                SizeSlider.Value = profile.Size;
                ThicknessSlider.Value = profile.Thickness;

                UpdateColorSelectionInUI();
                PopulateProfiles();

                foreach (ComboBoxItem item in PositionComboBox.Items)
                {
                    if (item.Tag?.ToString() == _settings.MenuPosition)
                    {
                        PositionComboBox.SelectedItem = item;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(profile.ValorantProfileCode))
                {
                    ValorantCodeBox.Text = profile.ValorantProfileCode;
                    _valorantProfile = ValorantProfile.Parse(profile.ValorantProfileCode);
                }
                else
                {
                    ValorantCodeBox.Text = "";
                    _valorantProfile = null;
                }
            }
            catch (Exception ex)
            {
                LogError("ApplySettings", ex);
            }
        }

        private void UpdateColorSelectionInUI()
        {
            try 
            {
                if (ColorComboBox == null) return;

                var profile = _settings.CurrentProfile;
                bool found = false;
                foreach (ComboBoxItem item in ColorComboBox.Items)
                {
                    if (item.Tag?.ToString() == profile.Color)
                    {
                        ColorComboBox.SelectedItem = item;
                        found = true;
                        break;
                    }
                }

                if (!found && !string.IsNullOrEmpty(profile.Color) && profile.Color.StartsWith("#"))
                {
                    var customItem = new ComboBoxItem { Content = $"Custom ({profile.Color})", Tag = profile.Color };
                    int insertIndex = Math.Max(0, ColorComboBox.Items.Count - 1);
                    ColorComboBox.Items.Insert(insertIndex, customItem);
                    ColorComboBox.SelectedItem = customItem;
                }
            }
            catch (Exception ex)
            {
                LogError("UpdateColorSelectionInUI", ex);
            }
        }

        private void PopulateProfiles()
        {
            if (ProfileComboBox == null) return;
            
            _isLoaded = false; // Temporarily disable change events
            ProfileComboBox.Items.Clear();
            foreach (var profile in _settings.Profiles)
            {
                ProfileComboBox.Items.Add(new ComboBoxItem { Content = profile.Name, Tag = profile.Name });
            }

            foreach (ComboBoxItem item in ProfileComboBox.Items)
            {
                if (item.Tag?.ToString() == _settings.CurrentProfileName)
                {
                    ProfileComboBox.SelectedItem = item;
                    break;
                }
            }
            _isLoaded = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try 
            {
                _hwnd = new WindowInteropHelper(this).Handle;
                RegisterHotKey(_hwnd, HOTKEY_ID, 0, VK_HOME);
                
                _settings = CrosshairSettings.Load();
                PopulateMonitors();
                ApplySettings();

                if (_settings.MonitorIndex >= 0 && _settings.MonitorIndex < Screen.AllScreens.Length)
                {
                    MonitorComboBox.SelectedIndex = _settings.MonitorIndex;
                    MoveToMonitor(Screen.AllScreens[_settings.MonitorIndex]);
                }
                else
                {
                    MoveToMonitor(Screen.PrimaryScreen!);
                }

                EnableClickThrough();

                ComponentDispatcher.ThreadFilterMessage += ComponentDispatcher_ThreadFilterMessage;
                _isLoaded = true;
                UpdateCrosshairLayout();
            }
            catch (Exception ex)
            {
                LogError("Window_Loaded", ex);
            }
        }

        private void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message == WM_HOTKEY && (int)msg.wParam == HOTKEY_ID)
            {
                ToggleMenu();
                handled = true;
            }
        }

        private void ToggleMenu()
        {
            if (SettingsMenu.Visibility == Visibility.Visible)
            {
                SettingsMenu.Visibility = Visibility.Collapsed;
                EnableClickThrough();
            }
            else
            {
                SettingsMenu.Visibility = Visibility.Visible;
                DisableClickThrough();
                this.Activate();
            }
        }

        private void EnableClickThrough()
        {
            if (_hwnd == IntPtr.Zero) return;
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST);
            SetWindowPos(_hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        private void DisableClickThrough()
        {
            if (_hwnd == IntPtr.Zero) return;
            int extendedStyle = GetWindowLong(_hwnd, GWL_EXSTYLE);
            SetWindowLong(_hwnd, GWL_EXSTYLE, (extendedStyle & ~WS_EX_TRANSPARENT) & ~WS_EX_NOACTIVATE);
        }

        private void PopulateMonitors()
        {
            try 
            {
                var screens = Screen.AllScreens;
                MonitorComboBox.Items.Clear();
                for (int i = 0; i < screens.Length; i++)
                {
                    MonitorComboBox.Items.Add(new ComboBoxItem 
                    { 
                        Content = $"Monitor {i + 1} ({(screens[i].Primary ? "Primary" : "Secondary")})",
                        Tag = screens[i]
                    });
                }
            }
            catch (Exception ex)
            {
                LogError("PopulateMonitors", ex);
            }
        }

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MonitorComboBox.SelectedItem is ComboBoxItem item && item.Tag is Screen screen)
            {
                _settings.MonitorIndex = MonitorComboBox.SelectedIndex;
                _settings.Save();
                MoveToMonitor(screen);
            }
        }

        private void MoveToMonitor(Screen screen)
        {
            try 
            {
                if (screen == null) return;

                if (_hwnd == IntPtr.Zero)
                {
                    this.Left = screen.Bounds.Left;
                    this.Top = screen.Bounds.Top;
                    this.Width = screen.Bounds.Width;
                    this.Height = screen.Bounds.Height;
                    return;
                }

                SetWindowPos(_hwnd, HWND_TOPMOST, screen.Bounds.Left, screen.Bounds.Top, screen.Bounds.Width, screen.Bounds.Height, SWP_NOACTIVATE | SWP_SHOWWINDOW);

                var dpi = VisualTreeHelper.GetDpi(this);
                this.Left = screen.Bounds.Left / dpi.DpiScaleX;
                this.Top = screen.Bounds.Top / dpi.DpiScaleY;
                this.Width = screen.Bounds.Width / dpi.DpiScaleX;
                this.Height = screen.Bounds.Height / dpi.DpiScaleY;

                if (_isLoaded) UpdateCrosshairLayout();
            }
            catch (Exception ex)
            {
                LogError("MoveToMonitor", ex);
            }
        }

        private void PositionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SettingsMenu == null) return;
            if (PositionComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string position = item.Tag.ToString() ?? "TopRight";
                
                if (_isLoaded)
                {
                    _settings.MenuPosition = position;
                    _settings.Save();
                }

                switch (position)
                {
                    case "TopRight":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Top;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                        break;
                    case "TopLeft":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Top;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        break;
                    case "BottomRight":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Bottom;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
                        break;
                    case "BottomLeft":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Bottom;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                        break;
                    case "Center":
                        SettingsMenu.VerticalAlignment = VerticalAlignment.Center;
                        SettingsMenu.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                        break;
                }
            }
        }

        private void ColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (HorizontalLine == null || VerticalLine == null) return;
            if (ColorComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                var tag = item.Tag.ToString();
                var profile = _settings.CurrentProfile;

                if (tag == "Custom")
                {
                    using (var dialog = new System.Windows.Forms.ColorDialog())
                    {
                        try 
                        {
                            var currentColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(profile.Color);
                            dialog.Color = System.Drawing.Color.FromArgb(currentColor.A, currentColor.R, currentColor.G, currentColor.B);
                        } catch {}

                        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            var color = dialog.Color;
                            string hex = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                            profile.Color = hex;
                            _settings.Save();
                            
                            UpdateColorSelectionInUI();
                            ApplyColor(hex);
                        }
                        else
                        {
                            UpdateColorSelectionInUI();
                        }
                    }
                    return;
                }

                profile.Color = tag ?? "Lime";
                _settings.Save();
                ApplyColor(profile.Color);
            }
        }

        private void ApplyColor(string colorStr)
        {
            try 
            {
                if (HorizontalLine == null || VerticalLine == null) return;
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorStr);
                var brush = new SolidColorBrush(color);
                HorizontalLine.Fill = brush;
                VerticalLine.Fill = brush;
            } catch {}
        }

        private void SettingsChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_isLoaded) return;

            var profile = _settings.CurrentProfile;
            profile.Size = SizeSlider.Value;
            profile.Thickness = ThicknessSlider.Value;
            _settings.Save();

            UpdateCrosshairLayout();
        }

        private void UpdateCrosshairLayout()
        {
            if (HorizontalLine == null || VerticalLine == null || CrosshairCanvas == null) return;

            double width = this.ActualWidth > 0 ? this.ActualWidth : this.Width;
            double height = this.ActualHeight > 0 ? this.ActualHeight : this.Height;
            if (double.IsNaN(width) || width <= 0) return;

            if (_valorantProfile != null)
            {
                RenderValorantCrosshair(width, height);
                return;
            }

            // Simple Mode
            HorizontalLine.Visibility = Visibility.Visible;
            VerticalLine.Visibility = Visibility.Visible;
            HideValorantElements();

            double size = SizeSlider.Value;
            double thickness = ThicknessSlider.Value;

            SizeLabel.Text = $"{(int)size}";
            ThicknessLabel.Text = $"{(int)thickness}";

            HorizontalLine.Width = size;
            HorizontalLine.Height = thickness;
            VerticalLine.Width = thickness;
            VerticalLine.Height = size;

            double centerX = width / 2;
            double centerY = height / 2;

            Canvas.SetLeft(HorizontalLine, centerX - (size / 2));
            Canvas.SetTop(HorizontalLine, centerY - (thickness / 2));

            Canvas.SetLeft(VerticalLine, centerX - (thickness / 2));
            Canvas.SetTop(VerticalLine, centerY - (size / 2));
        }

        private void RenderValorantCrosshair(double screenWidth, double screenHeight)
        {
            if (_valorantProfile == null) return;

            HorizontalLine.Visibility = Visibility.Collapsed;
            VerticalLine.Visibility = Visibility.Collapsed;

            var s = _valorantProfile.Primary;
            var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(_valorantProfile.GetColorHex());
            var brush = new SolidColorBrush(color);
            var outlineBrush = new SolidColorBrush(System.Windows.Media.Colors.Black) { Opacity = s.OutlineOpacity };
            
            double centerX = screenWidth / 2;
            double centerY = screenHeight / 2;

            // Center Dot
            if (s.CenterDotEnabled)
            {
                CenterDot.Visibility = Visibility.Visible;
                CenterDot.Width = s.CenterDotThickness;
                CenterDot.Height = s.CenterDotThickness;
                CenterDot.Fill = brush;
                CenterDot.Opacity = s.CenterDotOpacity;
                Canvas.SetLeft(CenterDot, centerX - (s.CenterDotThickness / 2.0));
                Canvas.SetTop(CenterDot, centerY - (s.CenterDotThickness / 2.0));

                if (s.OutlinesEnabled)
                {
                    CenterDotOutline.Visibility = Visibility.Visible;
                    CenterDotOutline.Width = s.CenterDotThickness + (s.OutlineThickness * 2);
                    CenterDotOutline.Height = s.CenterDotThickness + (s.OutlineThickness * 2);
                    CenterDotOutline.Fill = outlineBrush;
                    Canvas.SetLeft(CenterDotOutline, centerX - (CenterDotOutline.Width / 2.0));
                    Canvas.SetTop(CenterDotOutline, centerY - (CenterDotOutline.Height / 2.0));
                }
                else CenterDotOutline.Visibility = Visibility.Collapsed;
            }
            else
            {
                CenterDot.Visibility = Visibility.Collapsed;
                CenterDotOutline.Visibility = Visibility.Collapsed;
            }

            // Inner Lines
            RenderLines(s.InnerLines, centerX, centerY, brush, outlineBrush, s.OutlinesEnabled, s.OutlineThickness, 
                InnerLeft, InnerRight, InnerTop, InnerBottom, 
                InnerLeftOutline, InnerRightOutline, InnerTopOutline, InnerBottomOutline);

            // Outer Lines
            RenderLines(s.OuterLines, centerX, centerY, brush, outlineBrush, s.OutlinesEnabled, s.OutlineThickness, 
                OuterLeft, OuterRight, OuterTop, OuterBottom, 
                OuterLeftOutline, OuterRightOutline, OuterTopOutline, OuterBottomOutline);
        }

        private void RenderLines(ValorantProfile.LineSettings l, double centerX, double centerY, System.Windows.Media.Brush brush, System.Windows.Media.Brush outlineBrush, bool outlines, int outlineThickness,
            System.Windows.Shapes.Rectangle left, System.Windows.Shapes.Rectangle right, System.Windows.Shapes.Rectangle top, System.Windows.Shapes.Rectangle bottom,
            System.Windows.Shapes.Rectangle leftOut, System.Windows.Shapes.Rectangle rightOut, System.Windows.Shapes.Rectangle topOut, System.Windows.Shapes.Rectangle bottomOut)
        {
            if (!l.Show)
            {
                left.Visibility = right.Visibility = top.Visibility = bottom.Visibility = Visibility.Collapsed;
                leftOut.Visibility = rightOut.Visibility = topOut.Visibility = bottomOut.Visibility = Visibility.Collapsed;
                return;
            }

            left.Visibility = right.Visibility = top.Visibility = bottom.Visibility = Visibility.Visible;
            left.Fill = right.Fill = top.Fill = bottom.Fill = brush;
            left.Opacity = right.Opacity = top.Opacity = bottom.Opacity = l.Opacity;

            double hLen = l.Length;
            double vLen = l.Length;
            double thick = l.Thickness;
            double off = l.Offset;

            // Horizontal (Left/Right)
            left.Width = right.Width = hLen;
            left.Height = right.Height = thick;
            Canvas.SetTop(left, centerY - (thick / 2.0));
            Canvas.SetTop(right, centerY - (thick / 2.0));
            Canvas.SetLeft(left, centerX - off - hLen);
            Canvas.SetLeft(right, centerX + off);

            // Vertical (Top/Bottom)
            top.Width = bottom.Width = thick;
            top.Height = bottom.Height = vLen;
            Canvas.SetLeft(top, centerX - (thick / 2.0));
            Canvas.SetLeft(bottom, centerX - (thick / 2.0));
            Canvas.SetTop(top, centerY - off - vLen);
            Canvas.SetTop(bottom, centerY + off);

            if (outlines)
            {
                leftOut.Visibility = rightOut.Visibility = topOut.Visibility = bottomOut.Visibility = Visibility.Visible;
                leftOut.Fill = rightOut.Fill = topOut.Fill = bottomOut.Fill = outlineBrush;

                double outThick = thick + (outlineThickness * 2);
                double outHLen = hLen + (outlineThickness * 2);
                double outVLen = vLen + (outlineThickness * 2);

                leftOut.Width = rightOut.Width = outHLen;
                leftOut.Height = rightOut.Height = outThick;
                Canvas.SetTop(leftOut, centerY - (outThick / 2.0));
                Canvas.SetTop(rightOut, centerY - (outThick / 2.0));
                Canvas.SetLeft(leftOut, centerX - off - hLen - outlineThickness);
                Canvas.SetLeft(rightOut, centerX + off - outlineThickness);

                topOut.Width = bottomOut.Width = outThick;
                topOut.Height = bottomOut.Height = outVLen;
                Canvas.SetLeft(topOut, centerX - (outThick / 2.0));
                Canvas.SetLeft(bottomOut, centerX - (outThick / 2.0));
                Canvas.SetTop(topOut, centerY - off - vLen - outlineThickness);
                Canvas.SetTop(bottomOut, centerY + off - outlineThickness);
            }
            else
            {
                leftOut.Visibility = rightOut.Visibility = topOut.Visibility = bottomOut.Visibility = Visibility.Collapsed;
            }
        }

        private void HideValorantElements()
        {
            CenterDot.Visibility = CenterDotOutline.Visibility = Visibility.Collapsed;
            InnerLeft.Visibility = InnerRight.Visibility = InnerTop.Visibility = InnerBottom.Visibility = Visibility.Collapsed;
            InnerLeftOutline.Visibility = InnerRightOutline.Visibility = InnerTopOutline.Visibility = InnerBottomOutline.Visibility = Visibility.Collapsed;
            OuterLeft.Visibility = OuterRight.Visibility = OuterTop.Visibility = OuterBottom.Visibility = Visibility.Collapsed;
            OuterLeftOutline.Visibility = OuterRightOutline.Visibility = OuterTopOutline.Visibility = OuterBottomOutline.Visibility = Visibility.Collapsed;
        }

        private void ImportValorantCode_Click(object sender, RoutedEventArgs e)
        {
            string code = ValorantCodeBox.Text.Trim();
            if (string.IsNullOrEmpty(code)) return;

            _valorantProfile = ValorantProfile.Parse(code);
            _settings.CurrentProfile.ValorantProfileCode = code;
            _settings.Save();
            UpdateCrosshairLayout();
        }

        private void ClearValorantCode_Click(object sender, RoutedEventArgs e)
        {
            _valorantProfile = null;
            _settings.CurrentProfile.ValorantProfileCode = "";
            ValorantCodeBox.Text = "";
            _settings.Save();
            UpdateCrosshairLayout();
        }

        private void CopyValorantCode_Click(object sender, RoutedEventArgs e)
        {
            if (_valorantProfile != null)
            {
                System.Windows.Clipboard.SetText(_valorantProfile.ToCode());
            }
        }

        private void ProfileComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isLoaded) return;
            if (ProfileComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                string profileName = item.Tag.ToString()!;
                _settings.CurrentProfileName = profileName;
                _settings.Save();
                ApplySettings();
                UpdateCrosshairLayout();
            }
        }

        private void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            string name = "Profile " + (_settings.Profiles.Count + 1);
            _settings.Profiles.Add(new CrosshairProfile { Name = name });
            _settings.CurrentProfileName = name;
            _settings.Save();
            ApplySettings();
            UpdateCrosshairLayout();
        }

        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settings.Profiles.Count <= 1) return;

            var toDelete = _settings.CurrentProfile;
            _settings.Profiles.Remove(toDelete);
            _settings.CurrentProfileName = _settings.Profiles.First().Name;
            _settings.Save();
            ApplySettings();
            UpdateCrosshairLayout();
        }

        private void SaveAsProfileButton_Click(object sender, RoutedEventArgs e)
        {
            string newName = "Profile " + (_settings.Profiles.Count + 1);
            var newProfile = _settings.CurrentProfile.Clone(newName);
            _settings.Profiles.Add(newProfile);
            _settings.CurrentProfileName = newName;
            _settings.Save();
            ApplySettings();
            UpdateCrosshairLayout();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            UnregisterHotKey(_hwnd, HOTKEY_ID);
            ComponentDispatcher.ThreadFilterMessage -= ComponentDispatcher_ThreadFilterMessage;
            base.OnClosed(e);
        }
    }
}
