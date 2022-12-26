// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.



using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
            SignUpRequest user = new SignUpRequest() { Email = Email.Text, Name = Name.Text, Password = Password.Password };

            using var channel = GrpcChannel.ForAddress($"{Constants.URL}");
            var client = new AuthService.AuthServiceClient(channel);

            try
            {
                localSettings.Values["JwtToken"] = await client.SignUpAsync(user);
                MainWindow.NavigateToPage("SplashScreen");
            }
            catch (RpcException rpcException)
            {
                //TODO: Сделать перевод ошибок
                infoBar.Message = rpcException.Status.Detail;
                infoBar.IsOpen = true;
            }
        }

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigateToPage("Login");
        }
    }
}
