// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.


using CloudDrop.Helpers;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
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
            ProgressBar.Visibility = Visibility.Visible;
            SignInRequest request = new SignInRequest() { Email = Email.Text, Password = Password.Password };

            try {
                using var channel = GrpcChannel.ForAddress($"{Constants.URL}");
                var client = new AuthService.AuthServiceClient(channel);

                try {
                    var reply = await client.SignInAsync(request);
                    localSettings.Values["JwtToken"] = reply.Token;
                    ProgressBar.Visibility = Visibility.Collapsed;
                    MainWindow.NavigateToPage("SplashScreen");
                }
                catch (RpcException rpcException) {
                    ProgressBar.Visibility = Visibility.Collapsed;
                    infoBar.Message = rpcException.Status.Detail;
                    infoBar.IsOpen = true;
                }
            }
            catch
            {
                ProgressBar.Visibility = Visibility.Collapsed;
                ContentDialog ErrorDialog = new ContentDialog {
                    Title = "Error".GetLocalized(),
                    Content = "ErrorConnectBackend".GetLocalized(),
                    CloseButtonText = "Ok"
                };
                ErrorDialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
                await ErrorDialog.ShowAsync();
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.Visibility = Visibility.Collapsed;
            MainWindow.NavigateToPage("Registration");
        }

    }
}
