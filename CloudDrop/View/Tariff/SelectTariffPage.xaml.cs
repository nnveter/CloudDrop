// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
using CloudDrop.SplashScreen;
using CloudDrop.View.Dialogs;
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
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Protection.PlayReady;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.View.Tariff
{
    
    public sealed partial class SelectTariffPage : Page
    {
        //TODO

        public SelectTariffPage()
        {
            this.InitializeComponent();
            SetPlans();
        }


        public void SetPlans() 
        {
            PlansMessage plans = SplashScreenPage.plans;
            TariffName1.Text = plans.Plans[0].Name;
            TariffDescription1.Text = plans.Plans[0].Description + "\n11";
            PriceText1.Text = plans.Plans[0].Price.ToString() + " Руб/м";

            TariffName2.Text = plans.Plans[1].Name;
            TariffDescription2.Text = plans.Plans[1].Description + "\n11";
            PriceText2.Text = plans.Plans[1].Price.ToString() + " Руб/м";

            TariffName3.Text = plans.Plans[2].Name;
            TariffDescription3.Text = plans.Plans[2].Description;
            PriceText3.Text = plans.Plans[2].Price.ToString() + " Руб/м";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigateToPage("Files");
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ActiveCodeDialog dialog = new ActiveCodeDialog();
            dialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
            await dialog.ShowAsync();
            if (dialog.Statuscode == ActiveCodeStatus.Error)
            {
                ContentDialog ErrorDialog = new ContentDialog
                {
                    Title = "Activate code error",
                    Content = dialog.ExceptionCode.Status.Detail,
                    CloseButtonText = "Ok"
                };
                ErrorDialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
                await ErrorDialog.ShowAsync();
            }
            else if (dialog.Statuscode == ActiveCodeStatus.Success)
            {
                ContentDialog SuccessDialog = new ContentDialog
                {
                    Title = "Поздравляем!",
                    Content = "Ваша подписка активирована на 1 месяц",
                    CloseButtonText = "Ok"
                };
                SuccessDialog.XamlRoot = MainWindow.ContentFrame1.XamlRoot;
            }
        }
    }
}
