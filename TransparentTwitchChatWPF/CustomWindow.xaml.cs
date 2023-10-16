﻿using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Interop;
using Jot.DefaultInitializer;
using Microsoft.Web.WebView2.Core;
using Jot;
using System.IO;
using System.Diagnostics;

namespace TransparentTwitchChatWPF
{
    /// <summary>
    /// Interaction logic for CustomWindow.xaml
    /// </summary>
    public partial class CustomWindow : Window, BrowserWindow
    {
        MainWindow mainWindow;

        bool hiddenBorders = false;
        string customURL = "";
        string hashCode = "";

        TrackingConfiguration trackingConfig;

        [Trackable]
        public double ZoomLevel { get; set; }
        [Trackable]
        public string customCSS { get; set; }
        [Trackable]
        public string customJS { get; set; }

        public CustomWindow(MainWindow main, string Url, string CustomCSS)
        {
            this.mainWindow = main;
            this.customURL = Url;
            ZoomLevel = 1;
            customCSS = CustomCSS;
            customJS = "";

            InitializeComponent();

            hashCode = String.Format("{0:X}", this.customURL.GetHashCode());
            trackingConfig = Services.Tracker.Configure(this).IdentifyAs(hashCode);
            trackingConfig.Apply();

            InitializeWebViewAsync();
        }

        async void InitializeWebViewAsync()
        {
            var options = new CoreWebView2EnvironmentOptions("--autoplay-policy=no-user-gesture-required");
            string userDataFolder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TransparentTwitchChatWPF");

            CoreWebView2Environment cwv2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
            await webView.EnsureCoreWebView2Async(cwv2Environment);

            //this.jsCallbackFunctions = new JsCallbackFunctions();
            //webView.CoreWebView2.AddHostObjectToScript("jsCallbackFunctions", this.jsCallbackFunctions);

            SetupBrowser();
        }

        private void headerBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (e.ClickCount == 1)
                this.DragMove();
            else if (e.ClickCount == 2)
            {
                if (this.WindowState == WindowState.Maximized)
                    this.WindowState = WindowState.Normal;
            }
        }

        private void btnHide_Click(object sender, RoutedEventArgs e)
        {
            hideBorders();
        }

        public void drawBorders()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowExDefault(hwnd);

            btnClose.Visibility = Visibility.Visible;
            btnMin.Visibility = Visibility.Visible;
            btnMax.Visibility = Visibility.Visible;
            btnHide.Visibility = Visibility.Visible;
            //btnSettings.Visibility = System.Windows.Visibility.Visible;

            headerBorder.Background = Brushes.LightSlateGray;
            this.BorderBrush = Brushes.LightSlateGray;
            this.ResizeMode = System.Windows.ResizeMode.CanResizeWithGrip;

            hiddenBorders = false;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;

            this.webView.IsEnabled = true;
        }

        public void hideBorders()
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            WindowHelper.SetWindowExTransparent(hwnd);

            btnClose.Visibility = Visibility.Hidden;
            btnMin.Visibility = Visibility.Hidden;
            btnMax.Visibility = Visibility.Hidden;
            btnHide.Visibility = Visibility.Hidden;
            btnSettings.Visibility = Visibility.Hidden;

            headerBorder.Background = Brushes.Transparent;
            this.BorderBrush = Brushes.Transparent;
            this.ResizeMode = System.Windows.ResizeMode.NoResize;

            hiddenBorders = true;

            this.Topmost = false;
            this.Activate();
            this.Topmost = true;

            this.webView.IsEnabled = false;
        }

        public void ToggleBorderVisibility()
        {
            if (hiddenBorders)
                drawBorders();
            else
                hideBorders();
        }

        public void ResetWindowState()
        {
            drawBorders();
            this.WindowState = WindowState.Normal;
            this.Left = 10;
            this.Top = 10;
            this.Height = 450;
            this.Width = 300;
        }

        private void CommandBinding_CanExecute_1(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            if (MessageBox.Show("This will delete the settings for this window. Are you sure?", "Remove Window", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                this.mainWindow.RemoveCustomWindow(this.customURL);
                
                string path = (Services.Tracker.StoreFactory as Jot.Storage.JsonFileStoreFactory).StoreFolderPath;
                string jsonFile = Path.Combine(path, "CustomWindow_" + this.hashCode + ".json");
                Debug.WriteLine(jsonFile);

                trackingConfig.AutoPersistEnabled = false;
                
                if (File.Exists(jsonFile))
                {
                    try
                    {
                        File.Delete(jsonFile);
                    } catch { }
                }

                SystemCommands.CloseWindow(this);
            }
        }

        private void CommandBinding_Executed_3(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void SetupBrowser()
        {
            if (!string.IsNullOrWhiteSpace(this.customURL))
            {
                SetCustomAddress(this.customURL);
            }
        }

        private void SetCustomAddress(string url)
        {
            webView.CoreWebView2.Navigate(url);
        }

        private void ShowSettingsWindow(WindowSettings config)
        {
            /*SettingsWindow settingsWindow = new SettingsWindow(config);

            if (settingsWindow.ShowDialog() == true)
            {
                // update the AppSettings
                MessageBox.Show(config.URL);
            }*/
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            //WindowSettings cfg = new WindowSettings { isCustomURL = true, URL = this.customURL };
            //ShowSettingsWindow(cfg);
        }

        private void MenuItem_VisitWebsite(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/baffler/Transparent-Twitch-Chat-Overlay/releases");
        }

        private void MenuItem_ZoomIn(object sender, RoutedEventArgs e)
        {
            if (ZoomLevel < 4.0)
            {
                this.webView.ZoomFactor = ZoomLevel + 0.1;
                ZoomLevel = this.webView.ZoomFactor;
            }
        }

        private void MenuItem_ZoomOut(object sender, RoutedEventArgs e)
        {
            if (ZoomLevel > 0.1)
            {
                this.webView.ZoomFactor = ZoomLevel - 0.1;
                ZoomLevel = this.webView.ZoomFactor;
            }
        }

        private void MenuItem_ZoomReset(object sender, RoutedEventArgs e)
        {
            this.webView.ZoomFactor = 1;
            ZoomLevel = 1;
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        private void btnMax_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
                this.WindowState = WindowState.Normal;
            else
                this.WindowState = WindowState.Maximized;
        }

        private async void webView_NavigationCompleted(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            this.webView.Dispatcher.Invoke(new Action(() => { this.webView.ZoomFactor = ZoomLevel; }));

            // Insert some custom CSS for webcaptioner.com domain
            if (this.customURL.ToLower().Contains("webcaptioner.com"))
            {
                await this.webView.ExecuteScriptAsync(InsertCustomCSS(CustomCSS_Defaults.WebCaptioner));
            }

            if (!string.IsNullOrEmpty(this.customCSS))
            {
                await this.webView.ExecuteScriptAsync(InsertCustomCSS(this.customCSS));
            }
        }

        private string InsertCustomCSS(string CSS)
        {
            string uriEncodedCSS = Uri.EscapeDataString(CSS);
            string script = "const ttcCSS = document.createElement('style');";
            script += "ttcCSS.innerHTML = decodeURIComponent(\"" + uriEncodedCSS + "\");";
            script += "document.querySelector('head').appendChild(ttcCSS);";
            return script;
        }

        private void MenuItem_DevToolsClick(object sender, RoutedEventArgs e)
        {
            this.webView.CoreWebView2.OpenDevToolsWindow();
        }

        private void MenuItem_EditCSSClick(object sender, RoutedEventArgs e)
        {
            TextEditorWindow textEditorWindow = new TextEditorWindow(TextEditorType.CSS, this.customCSS);
            textEditorWindow.TextEdited += TextEditorWindow_TextEdited;
            textEditorWindow.Show();
        }

        private void TextEditorWindow_TextEdited(object sender, TextEditedEventArgs e)
        {
            this.customCSS = e.EditedText;
            this.trackingConfig.Persist(); // Save the changes
            this.webView.Reload();
        }
    }
}
