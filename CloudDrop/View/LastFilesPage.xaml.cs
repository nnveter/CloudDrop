// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CloudDrop.Models;
using CloudDrop.SplashScreen;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using static System.Net.WebRequestMethods;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CloudDrop.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LastFilesPage : Page
    {
        ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

        private List<int> _selectioneIndex = new List<int>();
        private List<Border> _selectioneBorder = new List<Border>();
        private bool _tap = false;
        public LastFilesPage()
        {
            this.InitializeComponent();

            Pro();
        }

        private void Pro()
        {
            var projects = new List<FileAr>();
            var newProject = new FileAr();

            //var localValue = localSettings.Values["JwtToken"] as string;

            List<Content> contents = new List<Content>();

            Content content = new Content() { contentType = ContentType.File, id = 1, name = "Style.css" };
            contents.Add(content);

            foreach (var item in contents)
            {
                newProject.Activities.Add(item);
            }

            projects.Add(newProject);

            Files.Source = projects;
        }

        private void Grid_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ClearSelection();
            _tap = false;
        }

        private void Border_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Border border = sender as Border;
            if (_selectioneBorder.IndexOf(border) == -1)
            {
                AddSelectionElement(border);
            }
            else 
            { 
                RemoveSelectionElement(border);
            }
            _tap = true;
        }

        private void Border_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ClearSelection();
            _tap = false;
        }

        private void AddSelectionElement(Border border) 
        {
            var Projects = (List<FileAr>)Files.Source;
            var file = (Models.Content)border.DataContext;

            if (_selectioneBorder.IndexOf(border) == -1)
            {
                _selectioneIndex.Add(Projects[0].Activities.IndexOf(file));
                _selectioneBorder.Add(border);

                border.Background = new SolidColorBrush(Color.FromArgb(23, 255, 255, 255));
                UpRow.Height = new GridLength(70);
                UpRow2.Height = new GridLength(0);

                if (_selectioneIndex.Count > 1)
                {
                    Header.Text = $"Ёлементов выбранно: {_selectioneIndex.Count}";
                }
                else
                {
                    Header.Text = file.name;
                }
            }
        }

        private void RemoveSelectionElement(Border border)
        {
            var Projects = (List<FileAr>)Files.Source;
            var file = (Models.Content)border.DataContext;
            if (_selectioneBorder.IndexOf(border) != -1)
            {
                _selectioneIndex.Remove(Projects[0].Activities.IndexOf(file));
                _selectioneBorder.Remove(border);
                border.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                if (_selectioneBorder.Count == 0 || _selectioneIndex.Count == 0) 
                {
                    UpRow.Height = new GridLength(0);
                    UpRow2.Height = new GridLength(70);
                    return;
                }

                if (_selectioneIndex.Count > 1)
                {
                    Header.Text = $"Ёлементов выбранно: {_selectioneIndex.Count}";
                }
                else
                {
                    Header.Text = file.name;
                }
            }
        }

        private void ClearSelection()
        {
            if (!_tap)
            {
                foreach (var item in _selectioneBorder)
                {
                    item.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
                    UpRow.Height = new GridLength(0);
                    UpRow2.Height = new GridLength(70);
                }
                _selectioneBorder.Clear();
                _selectioneIndex.Clear();
            }
        }
    }
    
    public class FileAr
    {
        public List<Content> Activities { get; set; } = new List<Content>();

    }
}
