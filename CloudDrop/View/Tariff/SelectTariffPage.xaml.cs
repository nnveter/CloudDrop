// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

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
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

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
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox txt = sender as TextBox;
            if (txt.Text.Length > 0 && !String.IsNullOrEmpty(txt.Text) && !String.IsNullOrWhiteSpace(txt.Text))
            {
                AcceptButton.Visibility = Visibility.Visible;
            }
            else 
            {
                AcceptButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

        }

        private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            string code = TextBoxCode.Text;

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.NavigateToPage("Files");
        }
    }
}
