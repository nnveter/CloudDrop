// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.Views.Autorization
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LoginPage : Page
    {
        ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        public LoginPage()
        {
            this.InitializeComponent();
        }

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            SignInRequest request = new SignInRequest() { Email = Email.Text, Password = Password.Password };
            using var channel = GrpcChannel.ForAddress($"http://localhost:5100");
            var client = new AuthService.AuthServiceClient(channel);

            try
            {
                var reply = await client.SignInAsync(request);
                localSettings.Values["JwtToken"] = reply.Token;
                MainWindow.NavigateToPage("SplashScreen");
            }
            catch (RpcException rpcException)
            {
                infoBar.Message = rpcException.Status.Detail;
                infoBar.IsOpen = true;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigateToPage("Registration");
        }

    }
}
