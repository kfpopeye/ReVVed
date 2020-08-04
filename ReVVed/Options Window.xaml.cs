using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RVVD
{
    /// <summary>
    /// Interaction logic for Options_Window.xaml
    /// </summary>
    public partial class Options_Window : Window
    {
        public Options_Window()
        {
            InitializeComponent();
            if (Properties.Settings.Default.LogFilePath == "none")
                tb_LogPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            else
                tb_LogPath.Text = Properties.Settings.Default.LogFilePath;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        /// <summary>
        /// Checks the ability to create and write to a file in the supplied directory.
        /// </summary>
        /// <param name="directory">String representing the directory path to check.</param>
        /// <returns>True if successful; otherwise false.</returns>
        private bool CheckDirectoryAccess(string directory)
        {
            const string TEMP_FILE = "\\tempFile.tmp";
            bool success = false;
            string fullPath = directory + TEMP_FILE;

            if (Directory.Exists(directory))
            {
                try
                {
                    using (FileStream fs = new FileStream(fullPath, FileMode.CreateNew,
                                                                    FileAccess.Write))
                    {
                        fs.WriteByte(0xff);
                    }

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        success = true;
                    }
                }
                catch (Exception)
                {
                    success = false;
                }
            }

            return success;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            global::RVVD.Properties.Settings.Default.Save();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            global::RVVD.Properties.Settings.Default.Save();
            this.Close();
        }

        private void tb_LogPath_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.RootFolder = Environment.SpecialFolder.MyComputer;
            System.Windows.Forms.DialogResult res = fbd.ShowDialog();
            if (res != System.Windows.Forms.DialogResult.OK)
                return;

            if (CheckDirectoryAccess(fbd.SelectedPath))
            {
                tb_LogPath.Text = fbd.SelectedPath;
                Properties.Settings.Default.LogFilePath = tb_LogPath.Text;
            }
            else
                Autodesk.Revit.UI.TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("OPT_DIA001_Title"), //Directory error
                    LocalizationProvider.GetLocalizedValue<string>("OPT_DIA001_MainInstr")); //You do not have permission to write to this directory.
        }
    }

    public class SettingBindingExtension : Binding
    {
        public SettingBindingExtension()
        {
            Initialize();
        }

        public SettingBindingExtension(string path)
            : base(path)
        {
            Initialize();
        }

        private void Initialize()
        {
            this.Source = global::RVVD.Properties.Settings.Default;
            this.Mode = BindingMode.TwoWay;
        }
    }
}
