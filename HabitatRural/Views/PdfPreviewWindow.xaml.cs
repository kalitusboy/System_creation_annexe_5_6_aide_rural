using System;
using System.Windows;
using System.Windows.Controls;

namespace HabitatRural.Views
{
    public partial class PdfPreviewWindow : Window
    {
        private string _pdfPath;
        public PdfPreviewWindow(string pdfPath)
        {
            InitializeComponent();
            _pdfPath = pdfPath;
            Loaded += (s, e) => WebBrowser.Navigate(pdfPath);
        }
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("يمكنك حفظ الملف وطباعته من قارئ PDF.", "طباعة");
        }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
