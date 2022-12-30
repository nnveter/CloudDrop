// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
using CloudDrop.SplashScreen;
using CloudDrop.Views.Account;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics.Metrics;
using System.Globalization;
using Windows.Storage;

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
                // TODO
            }
        }

        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dateTime;
        }
    }
}
