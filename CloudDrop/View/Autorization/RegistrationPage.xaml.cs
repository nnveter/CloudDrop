// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.



using CloudDrop.Helpers;
using CloudDrop.SplashScreen;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Globalization;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.Views.Autorization
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RegistrationPage : Page
    {
        ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
        public RegistrationPage()
        {
            this.InitializeComponent();
        }

        private async void myButton_Click(object sender, RoutedEventArgs e)
        {
            ProgressBar.Visibility = Visibility.Visible;
            CultureInfo currentCulture = CultureInfo.CurrentCulture;        
            RegionInfo currentRegion = new RegionInfo(currentCulture.Name);
            var country = currentRegion.DisplayName;

            UserInfoMessage message = new UserInfoMessage() { Country = country, FirstName = "", LastName = "", City = "" };
            SignUpRequest user = new SignUpRequest() { Email = Email.Text, Name = Name.Text, Password = Password.Password };

            try {
                using var channel = GrpcChannel.ForAddress($"{Constants.URL}");
                var client = new AuthService.AuthServiceClient(channel);

                try {
                    TokenResponse token = await client.SignUpAsync(user);
                    localSettings.Values["JwtToken"] = token.Token;

                    using var channel2 = GrpcChannel.ForAddress($"{Constants.URL}");
                    var client2 = new UsersService.UsersServiceClient(channel2);

                    var headers = new Metadata();
                    headers.Add("authorization", $"Bearer {token.Token}");

                    client2.UpdateProfileInfo(message, headers);

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
            MainWindow.NavigateToPage("Login");
        }
    }
}
