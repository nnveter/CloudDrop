// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
using CloudDrop.SplashScreen;
using CloudDrop.Views.Account;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics.Metrics;
using System.Globalization;
using Windows.Storage;
using WinUIEx.Messaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.View.Account
{
    public sealed partial class AccountPage : Page
    {
        public User ViewModel;
        public SubscriptionMessage Subscription;
        public SubscriptionPlanMessage SubscriptionPlan;
        public AccountPage()
        {
            this.InitializeComponent();

            ViewModel = SplashScreenPage.user;
            Subscription = SplashScreenPage.subscription;
            SubscriptionPlan = Subscription.Plan;
            TariffDate.Text = UnixTimeStampToDateTime(Subscription.FinishAt).ToShortDateString();
        }

        private void ButtonLogOut_Click(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["JwtToken"] = null;
            MainWindow.NavigateToPage("Login");
        }

        private async void GridV_ItemClick(object sender, ItemClickEventArgs e)
        {
            OpenUserData dialog = new OpenUserData();
            dialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
            await dialog.ShowAsync();
            User user = dialog.user;
            if (user != SplashScreenPage.user && user != null)
            {
                SplashScreenPage.user = user;
                ViewModel = user;

                LastName.UpdateLayout();
                FirsName.UpdateLayout();

                using var channel = GrpcChannel.ForAddress($"{Constants.URL}");
                var client = new UsersService.UsersServiceClient(channel);

                UserInfoMessage message = new UserInfoMessage() { Country = dialog.user.country, City = dialog.user.city, FirstName = dialog.user.name, LastName = dialog.user.lastName };

                ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                var token = localSettings.Values["JwtToken"];

                var headers = new Metadata();
                headers.Add("authorization", $"Bearer {token}");

                await client.UpdateProfileInfoAsync(message, headers);
            }
        }

        public static DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }

        private void GridV_ItemClick_1(object sender, ItemClickEventArgs e)
        {
            MainWindow.NavigateToPage("SelectTariff");
        }
    }
}
