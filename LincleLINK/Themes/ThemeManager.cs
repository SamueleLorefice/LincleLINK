using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;

namespace LincleLINK
{
    public static class ThemeManager
    {
        public static bool IsDark { get; private set; }

        private static readonly string settingsPath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");

        static ThemeManager()
        {
            try
            {
                if (File.Exists(settingsPath))
                {
                    using FileStream fs = File.OpenRead(settingsPath);
                    Settings? s = JsonSerializer.Deserialize<Settings>(fs);
                    IsDark = s != null && s.IsDarkTheme;
                }
            }
            catch { }
        }

        public static void ApplyTheme(bool dark)
        {
            IsDark = dark;

            var dicts = Application.Current.Resources.MergedDictionaries;
            dicts.Clear();
            var theme = new ResourceDictionary
            {
                Source = new Uri(dark
                    ? "/LincleLINK;component/Themes/DarkTheme.xaml"
                    : "/LincleLINK;component/Themes/LightTheme.xaml", UriKind.Relative)
            };
            dicts.Add(theme);

            foreach (Window window in Application.Current.Windows)
            {
                ApplyImmersiveTitleBar(window);
            }

            SaveSettings();
        }

        public static void ApplyImmersiveTitleBar(Window window)
        {
            try
            {
                var hwnd = new WindowInteropHelper(window).Handle;
                if (hwnd == IntPtr.Zero)
                {
                    return;
                }

                int value = IsDark ? 1 : 0;
                int attr = 20; // DWMWA_USE_IMMERSIVE_DARK_MODE (Windows 10 1903+)
                if (DwmSetWindowAttribute(hwnd, attr, ref value, sizeof(int)) != 0)
                {
                    attr = 19; // pre-1903 fallback
                    DwmSetWindowAttribute(hwnd, attr, ref value, sizeof(int));
                }
            }
            catch { }
        }

        private static void SaveSettings()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(settingsPath, JsonSerializer.Serialize(new Settings { IsDarkTheme = IsDark }, options));
            }
            catch { }
        }

        private class Settings
        {
            public bool IsDarkTheme { get; set; }
        }

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
