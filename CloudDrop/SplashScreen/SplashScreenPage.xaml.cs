// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using Windows.Storage;
using Grpc.Core;
using Grpc.Net.Client;
using System.Threading.Tasks;
using CloudDrop.Models;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.SplashScreen
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SplashScreenPage : Page
    {
        public static User user;
        public SplashScreenPage()
        {
            this.InitializeComponent();
            MainVoid();
        }

        public async static Task<bool> MainVoid()
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            String Token = localSettings.Values["JwtToken"] as string;

            if (await IsCheckedAuthorization(Token))
            {
                MainWindow.NavigateToPage("LastFiles");
                return true;
            }
            else
            {
                MainWindow.NavigateToPage("Login");
                return false;
            }
        }

        public async static Task<bool> IsCheckedAuthorization(String JwtTocken)
        {
            var headers = new Metadata
            {
                { "authorization", $"Bearer {JwtTocken}" }
            };

            using (var channel = GrpcChannel.ForAddress(Constants.URL))
            {
                var client = new UsersService.UsersServiceClient(channel);
                try
                {
                    user = await client.GetProfileAsync(new UsersEmptyMessage(), headers);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }
    }
}
