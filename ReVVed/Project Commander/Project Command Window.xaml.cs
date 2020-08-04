using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;

using Autodesk.Revit.UI;

namespace RVVD.Project_Commander
{
    /// <summary>
    /// Interaction logic for Project_Command_Window.xaml
    /// </summary>
    public partial class Project_Command_Window : Window
    {
        private string m_XMLfilepath = string.Empty;
        private string m_projectName = string.Empty;
        private string m_projectID = string.Empty;
        private ProjectCommander m_DataSet = null;
        private CollectionViewSource m_viewSource = null;
        private bool m_hasChanges = false;

        private string safeProjectName
        {
            get { return pkhCommon.StringHelper.SafeForXML(m_projectName); }
        }

        public Project_Command_Window(string xml_file)
        {
            InitializeComponent();
            m_XMLfilepath = xml_file;
            m_DataSet = this.FindResource("projectCommander") as ProjectCommander;
            m_viewSource = this.FindResource("projectViewSource") as CollectionViewSource;
        }

        public bool setProject(Autodesk.Revit.DB.ProjectInfo projectinfo)
        {
            //extract project info
            m_projectName = projectinfo.Number + " " + projectinfo.Name;
            tb_Project.Text = m_projectName;
            tb_Issue.Text = projectinfo.IssueDate;
            tb_Status.Text = projectinfo.Status;
            PC_tb_UF1.Text = RVVD.Properties.Settings.Default.PC_UserField1;
            PC_tb_UF2.Text = RVVD.Properties.Settings.Default.PC_UserField2;

            m_DataSet.Clear();
            m_DataSet.ReadXml(m_XMLfilepath);
            m_DataSet.AcceptChanges();

            // Moves the viewsource to the current project.
            bool result = false;

            if (!m_viewSource.View.MoveCurrentToFirst())
                return false;

            do
            {
                System.Data.DataRowView pr = m_viewSource.View.CurrentItem as System.Data.DataRowView;
                if (pr.Row.ItemArray[2].ToString() != safeProjectName)
                    m_viewSource.View.MoveCurrentToNext();
                else
                {
                    System.Data.DataRowView row = m_viewSource.View.CurrentItem as System.Data.DataRowView;
                    m_projectID = row.Row.ItemArray[3].ToString(); //set project id #
                    result = true;
                }

            } while (!m_viewSource.View.IsCurrentAfterLast && result == false);

            return result;
        }

        private void b_Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (m_hasChanges)
            {
                TaskDialog td = new TaskDialog("Warning");
                td.MainInstruction = "This will not save changes you have made. Are you sure this is what you want?";
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Yes");
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "No");
                TaskDialogResult tdr = td.Show();

                if (tdr == TaskDialogResult.CommandLink1)
                    this.Close();
            }
            else
                this.Close();
        }

        private void b_AddMilestone_Click(object sender, RoutedEventArgs e)
        {
            Milestone_Window mw = new Milestone_Window();
            mw.Owner = this;
            mw.ShowDialog();

            if ((bool)mw.DialogResult)
            {
                lv_Milestones.BeginInit();
                ProjectCommander.MilestoneItemRow row = m_DataSet.MilestoneItem.NewRow() as ProjectCommander.MilestoneItemRow;
                row["Date"] = mw.dateTimePicker;
                row["Event"] = mw.textBox_Milestone;
                row["project_ID"] = m_projectID;
                m_DataSet.MilestoneItem.AddMilestoneItemRow(row);
                m_DataSet.AcceptChanges();
                lv_Milestones.EndInit();
                m_hasChanges = true;
            }
        }

        private void b_DeleteMilestone_Click(object sender, RoutedEventArgs e)
        {
            if (lv_Milestones.SelectedItem != null)
            {
                lv_Milestones.BeginInit();
                System.Data.DataRowView drv = lv_Milestones.SelectedItem as System.Data.DataRowView;
                ProjectCommander.MilestoneItemRow mir = drv.Row as ProjectCommander.MilestoneItemRow;
                m_DataSet.MilestoneItem.RemoveMilestoneItemRow(mir);
                m_DataSet.AcceptChanges();
                lv_Milestones.EndInit();
                m_hasChanges = true;
            }
        }

        private void b_AddComment_Click(object sender, RoutedEventArgs e)
        {
            lvCommentItems.BeginInit();
            ProjectCommander.CommentItemRow row = m_DataSet.CommentItem.NewRow() as ProjectCommander.CommentItemRow;
            row["Date"] = System.DateTime.Today.ToString("d");
            row["Description"] = "{\\rtf1\\ansi\\ansicpg1252\\deff0\\deflang1033{\\fonttbl{\\f0\\fnil\\fcharset0 Microsoft Sans Serif;}}\\viewkind4\\uc1\\pard\\f0\\fs17 New comment.\\par}";
            row["project_ID"] = m_projectID;
            m_DataSet.CommentItem.AddCommentItemRow(row);
            m_DataSet.AcceptChanges();
            lvCommentItems.EndInit();
            lvCommentItems.SelectedIndex = lvCommentItems.Items.Count - 1;
            m_hasChanges = true;
        }

        private void b_DeleteComment_Click(object sender, RoutedEventArgs e)
        {
            if (lvCommentItems.SelectedItem != null)
            {
                lvCommentItems.BeginInit();
                System.Data.DataRowView drv = lvCommentItems.SelectedItem as System.Data.DataRowView;
                ProjectCommander.CommentItemRow mir = drv.Row as ProjectCommander.CommentItemRow;
                m_DataSet.CommentItem.RemoveCommentItemRow(mir);
                m_DataSet.AcceptChanges();
                lvCommentItems.EndInit();
                m_hasChanges = true;
            }
        }

        private void b_Help_Click(object sender, RoutedEventArgs e)
        {
            global::RVVD.Chelp.pc_help.Launch();
        }

        private void b_OK_Click(object sender, RoutedEventArgs e)
        {
            m_DataSet.AcceptChanges();
            System.IO.StreamWriter sw = new System.IO.StreamWriter(m_XMLfilepath);
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
            AdornerLayer al = AdornerLayer.GetAdornerLayer(splitter);
            al.Add(new DoubleArrowAdorner(splitter));

            DescColumn.Width = lvCommentItems.ActualWidth - lv1gv.Columns[0].ActualWidth -10;
            milestoneColumn.Width = lv_Milestones.ActualWidth - lvgv.Columns[0].ActualWidth - 10;
        }

        private void lvCommentItems_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            DescColumn.Width = lvCommentItems.ActualWidth - lv1gv.Columns[0].ActualWidth - 10;
            milestoneColumn.Width = lv_Milestones.ActualWidth - lvgv.Columns[0].ActualWidth - 10;
        }
    }   
}
