using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RVVD.Project_Commander
{
    /// <summary>
    /// Interaction logic for Project_Manager_Window.xaml
    /// </summary>
    public partial class Project_Manager_Window : Window
    {
        private string _projectName;
        private string _xmlfile;
        private ProjectCommander m_DataSet = null;
        private CollectionViewSource m_viewSource = null;

        /// <summary>
        /// Used when selecting a project that should be renamed (existing project does not exist dialog)
        /// </summary>
        public Project_Manager_Window(string xmlFile, string p)
        {
            InitializeComponent();
            //set global vars
            _xmlfile = xmlFile;
            _projectName = p;

            tb_ProjName.Text = _projectName;
            Setup();
        }

        /// <summary>
        /// Used when pruning projects from the database (Project Manager from Revit ribbon)
        /// </summary>
        public Project_Manager_Window(string xmlFile)
        {
            InitializeComponent();
            //set global vars
            _xmlfile = xmlFile;

            tb_ProjName.Visibility = Visibility.Collapsed;
            PC_PMW_tb_Instruction.Text = LocalizationProvider.GetLocalizedValue<string>("PC_PMW_tb_Instruction2"); //Select which projects you would like to delete from the database.
            bt_Select.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Visible;
            CloseButton.Visibility = Visibility.Visible;
            Setup();
        }

        private void Setup()
        {
            m_DataSet = this.FindResource("projectCommander") as ProjectCommander;
            m_viewSource = this.FindResource("projectViewSource") as CollectionViewSource;
            m_DataSet.Clear();
            m_DataSet.ReadXml(_xmlfile);
            m_DataSet.AcceptChanges();
        }

        private void bt_Select_Click(object sender, RoutedEventArgs e)
        {
            //renumber selected project to new name
            
            DataRow row = m_DataSet.Project.Rows[lv_Projects.SelectedIndex];
            row["ProjectID"] = pkhCommon.StringHelper.SafeForXML(_projectName);
            m_DataSet.AcceptChanges();

            System.IO.StreamWriter sw = new System.IO.StreamWriter(_xmlfile);
            m_DataSet.WriteXml(sw);
            sw.Flush();
            sw.Dispose();
            DialogResult = true;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            //delete selected project
            Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("PC_PMW_DIA001_Title")); //Confirm delete
            td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("PC_PMW_DIA001_MainInstr"); // This will permanently delete all comments, milestones and information stored in the database for this project. Are you sure this is what you want?
            td.MainIcon = Autodesk.Revit.UI.TaskDialogIcon.TaskDialogIconWarning;
            td.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("TXT_yes"));
            td.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("TXT_no"));
            Autodesk.Revit.UI.TaskDialogResult tdr = td.Show();

            if (tdr == Autodesk.Revit.UI.TaskDialogResult.CommandLink1)
            {
                if (lv_Projects.SelectedItem != null)
                {
                    lv_Projects.BeginInit();
                    DataRowView drv = lv_Projects.SelectedItem as DataRowView;
                    ProjectCommander.ProjectRow mir = drv.Row as ProjectCommander.ProjectRow;
                    m_DataSet.Project.RemoveProjectRow(mir);
                    m_DataSet.AcceptChanges();
                    lv_Projects.EndInit();
                }
            }
        }

        private void bt_Close_Click(object sender, RoutedEventArgs e)
        {
            m_DataSet.AcceptChanges();
            System.IO.StreamWriter sw = new System.IO.StreamWriter(_xmlfile);
            m_DataSet.WriteXml(sw);
            sw.Flush();
            sw.Dispose();
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            this.Title += " - DEBUG MODE";
#endif
        }
    }
}
