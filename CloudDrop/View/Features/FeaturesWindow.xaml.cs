// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;
using WinRT;
using CloudDrop.Models;
using Microsoft.UI.Xaml.Media.Imaging;
using CloudDrop.SplashScreen;
using Windows.UI.WindowManagement;
using Windows.UI.ViewManagement;
using Grpc.Core;
using Grpc.Net.Client;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.View
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FeaturesWindow : WindowEx
    {
        WindowsSystemDispatcherQueueHelper m_wsdqHelper; // See separate sample below for implementation
        Microsoft.UI.Composition.SystemBackdrops.MicaController m_micaController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        public Content content1;
        public List<string> allNameFiles;
        public FeaturesWindow(Content content, List<string> AllNameFiles)
        {
            this.InitializeComponent();
            TrySetMicaBackdrop();
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            content1 = content;
            allNameFiles = AllNameFiles;

            if (content.contentType == ContentType.Folder) 
            { 
                Type.Text = "Folder"; 
            }
            else 
            {
                Type.Text = content.type;
            }

            SetSize(content);

            TitleImage.Source = new BitmapImage(new Uri(content.Icon));
            Icon.Source = new BitmapImage(new Uri(content.Icon));
            Path.Text = content.path;
            Date.Text = content.CreateAt;
            TextBoxName.Text = content.name;
        }

        private void TextBoxName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBoxName.Text != content1.name 
                && TextBoxName.Text.Count() > 0 
                && allNameFiles.IndexOf(TextBoxName.Text) == -1 
                && !string.IsNullOrEmpty(TextBoxName.Text) 
                && !string.IsNullOrWhiteSpace(TextBoxName.Text))
            {
                Applybutton.IsEnabled = true;
            }
            else
            {
                Applybutton.IsEnabled = false;
            }
        }

        private void CancelButton(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void Applybutton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            try
            {
                var channel = GrpcChannel.ForAddress(Constants.URL);
                var client = new ContentsService.ContentsServiceClient(channel);

                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                String Token = localSettings.Values["JwtToken"] as string;

                var headers = new Metadata();
                headers.Add("authorization", $"Bearer {Token}");

                await client.RenameContentAsync(new RenameContentRequest() { ContentId = content1.id, NewName = TextBoxName.Text }, headers);
                await channel.ShutdownAsync();
            }
            catch 
            { 
            }

            this.Close();
        }

        private void SetSize(Content content) 
        {
            if (content.size != null)
            {
                double bi = (double)content.size;

                int b = (int)Math.Round(((double)bi), 2);
                if (b < 1024)
                {
                    Size.Text = b + " byte";
                    return;
                }

                int kb = (int)Math.Round(((double)(b / 1024)), 2);
                if (kb < 1024)
                {
                    Size.Text = kb + " Kb";
                    return;
                }

                int mb = (int)Math.Round(((double)(kb / 1024)), 2);
                if (mb < 1024)
                {
                    Size.Text = mb + " MB";
                    return;
                }

                int gb = (int)Math.Round(((double)(mb / 1024)), 2);
                if (gb < 1024)
                {
                    Size.Text = gb + " GB";
                    return;
                }
            }
        }

















        //Thems
        bool TrySetMicaBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Hooking up the policy object
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Mica is not supported on this system
        }
        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }
        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }
    }

    class WindowsSystemDispatcherQueueHelper
    {
        [StructLayout(LayoutKind.Sequential)]
        struct DispatcherQueueOptions
        {
            internal int dwSize;
            internal int threadType;
            internal int apartmentType;
        }

        [DllImport("CoreMessaging.dll")]
        private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

        object m_dispatcherQueueController = null;
        public void EnsureWindowsSystemDispatcherQueueController()
        {
            if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
            {
                // one already exists, so we'll just use it.
                return;
            }

            if (m_dispatcherQueueController == null)
            {
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref m_dispatcherQueueController);
            }
        }
    }
}
