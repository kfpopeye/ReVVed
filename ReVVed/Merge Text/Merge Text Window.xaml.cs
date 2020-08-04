using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace RVVD.Merge_Text
{
    /// <summary>
    /// Interaction logic for Merge_Text_Window.xaml
    /// </summary>
    public partial class Merge_Text_Window : Window
    {
        public System.Collections.ObjectModel.ObservableCollection<Element> _notesList = null;
        Func<string, string> _caseChanger = null;
        FlowDocument mergedDocument = null;
        private string theFont = string.Empty;

        public FlowDocument get_MergedDocument()
        {
            return preview_window.Document;
        }

        public Merge_Text_Window(Text_Elements te)
        {
            InitializeComponent();
#if !DEBUG
            MT_bt_ViewXML.Visibility = Visibility.Collapsed;  //hide button for viewing the raw xml in the preview window. for debugging only
#endif
            theFont = te.theFont;
            _notesList = te._theList;
            noteListBox.ItemsSource = _notesList;
            update_preview();
        }

        private void cb_Click(object sender, RoutedEventArgs e)
        {
            update_preview();
        }

        private void cbCase_Click(object sender, SelectionChangedEventArgs e)
        {
            if (cb_Case.SelectedItem == null)
                return;

            ComboBoxItem cbi = cb_Case.SelectedItem as ComboBoxItem;
            var textInfo = System.Globalization.CultureInfo.CurrentUICulture.TextInfo;

            if (cbi == null || (string)cbi.Tag == "none")
            {
                _caseChanger = null;
            }
            else if ((string)cbi.Tag == "title")
            {
                _caseChanger = (text) => textInfo.ToTitleCase(textInfo.ToLower(text));
            }
            else if ((string)cbi.Tag == "upper")
            {
                _caseChanger = (text) => textInfo.ToUpper(text);
            }
            else if ((string)cbi.Tag == "lower")
            {
                _caseChanger = (text) => textInfo.ToLower(text);
            }
            else if ((string)cbi.Tag == "toggle")
            {
                _caseChanger = (text) => pkhCommon.StringHelper.ToggleCase(text);
            }
            else if ((string)cbi.Tag == "sentance")
            {
                _caseChanger = (text) => pkhCommon.StringHelper.SentenceCase(text);
            }
            else
            {
                _caseChanger = null;
            }

            update_preview();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AdornerLayer al = AdornerLayer.GetAdornerLayer(splitter);
            al.Add(new DoubleArrowAdorner(splitter));
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            RVVD.Chelp.mt_help.Launch();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            int x = noteListBox.SelectedIndex;

            if (x > 0)
            {
                noteListBox.BeginInit();
                Element el = _notesList[x];
                _notesList.RemoveAt(x);
                _notesList.Insert(x - 1, el);
                noteListBox.EndInit();
                update_preview();
                if (x - 1 <= 0)
                    UpButton.IsEnabled = false;
                DownButton.IsEnabled = true;
            }
        }

        private void noteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (noteListBox.SelectedIndex == 0)
                UpButton.IsEnabled = false;
            else
                UpButton.IsEnabled = true;
            if (noteListBox.SelectedIndex == noteListBox.Items.Count - 1)
                DownButton.IsEnabled = false;
            else
                DownButton.IsEnabled = true;
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            int x = noteListBox.SelectedIndex;

            if (x != -1 && x < noteListBox.Items.Count - 1)
            {
                noteListBox.BeginInit();
                Element el = _notesList[x];
                _notesList.RemoveAt(x);
                _notesList.Insert(x + 1, el);
                noteListBox.EndInit();
                update_preview();
                if (x >= noteListBox.Items.Count - 1)
                    DownButton.IsEnabled = false;
                UpButton.IsEnabled = true;
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_notesList.Count <= 2)
                return;

            Element el = null;

            if (e != null)
            {
                Button btn = e.Source as Button;
                var q = _notesList.Where(X => X._theID == (int)btn.Tag).FirstOrDefault();
                if (q != null)
                    el = q as Element;
            }
            else
                el = noteListBox.SelectedItem as Element;

            if (el != null)
            {
                noteListBox.BeginInit();
                _notesList.Remove(el);
                noteListBox.EndInit();
                update_preview();
            }
        }

        private void noteList_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.NumPad8 || e.Key == Key.Up)
                UpButton_Click(null, null);
            else if (e.Key == Key.NumPad2 || e.Key == Key.Down)
                DownButton_Click(null, null);
            else if (e.Key == Key.Delete)
                RemoveButton_Click(null, null);
        }

        private void FlowDocumentScrollViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            FlowDocumentScrollViewer v = sender as FlowDocumentScrollViewer;
            v.Document = null;
        }

        private void FlowDocumentScrollViewer_Loaded(object sender, RoutedEventArgs e)
        {
            FlowDocumentScrollViewer v = sender as FlowDocumentScrollViewer;
            v.SetBinding(FlowDocumentScrollViewer.DocumentProperty, new System.Windows.Data.Binding("_theDoc"));
        }

        private void ViewXML_Click(object sender, RoutedEventArgs e)
        {
            using (System.IO.FileStream fs = new System.IO.FileStream(@"C:\Users\User\Desktop\viewXML.xml", System.IO.FileMode.Create))
            {
                TextRange range = new TextRange(mergedDocument.ContentStart, mergedDocument.ContentEnd);
                range.Save(fs, DataFormats.Xaml);
                System.Diagnostics.Process.Start(@"C:\Program Files (x86)\Notepad++\notepad++.exe", @"C:\Users\User\Desktop\viewXML.xml");
            }
        }
    }
}
