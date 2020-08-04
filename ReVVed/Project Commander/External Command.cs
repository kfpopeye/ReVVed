using System;
using System.IO;
using System.Windows.Forms;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RVVD
{
    /// <summary>
    /// Show dockable dialog
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class ShowDockableWindow : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            DockablePaneId dpid = ReVVed.getProjCommanderDockPaneId();
            DockablePane dp = commandData.Application.GetDockablePane(dpid);
            dp.Show();
            return Result.Succeeded;
        }
    }

    /// <summary>
    /// Hide dockable dialog
    /// </summary>
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class HideDockableWindow : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            DockablePaneId dpid = ReVVed.getProjCommanderDockPaneId();
            DockablePane dp = commandData.Application.GetDockablePane(dpid);
            dp.Hide();
            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class project_commander : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(project_commander));
        private readonly string XMLfilepath = Constants.RevvedBasePath + "ProjectCommander.xml";

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /*
             * To display a dialog that will show:
             *      -project name and number
             *      -has 2 user defineable fields
             *      -list upcoming milestones (DP,BP,IFC)
             *      -project comments in rich text format
             *      
             * Design for single person use at this point.
             */
            Document actdoc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                //using (Measurement_Protocol mp = new Measurement_Protocol())
                //{
                //    mp.sendAnalytic(Constants.PROJ_COMM_MACRO_NAME, commandData);
                //}
                //if XML does not exist, create a new one

                if (!System.IO.File.Exists(XMLfilepath))
                {
                    log.Info("Could not find xml database file. Creating a new one.");
                    if (!System.IO.Directory.Exists(Constants.RevvedBasePath))
                        System.IO.Directory.CreateDirectory(Constants.RevvedBasePath);

                    System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
                    doc.LoadXml("<?xml version=\"1.0\" encoding=\"utf-8\" ?><ProjectCommander><Project ProjectID=\"Blank Project\"></Project></ProjectCommander>");
                    doc.Save(XMLfilepath);
                }

                Project_Commander.Project_Command_Window pcw = new Project_Commander.Project_Command_Window(XMLfilepath);
                IntPtr handle = commandData.Application.MainWindowHandle;
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(pcw);
                wih.Owner = handle;
                if (pcw.setProject(commandData.Application.ActiveUIDocument.Document.ProjectInformation))
                    pcw.ShowDialog();
                else if (prompt_for_project(commandData))
                {
                    pcw.setProject(commandData.Application.ActiveUIDocument.Document.ProjectInformation);
                    pcw.ShowDialog();
                }
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception.", err);

                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("MT_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());
            }

            return Result.Cancelled;
        }

        private bool prompt_for_project(ExternalCommandData commandData)
        {
            ProjectInfo projectinfo = commandData.Application.ActiveUIDocument.Document.ProjectInformation;
            Project_Commander.ProjectCommander m_DataSet = null;
            string m_projectName = projectinfo.Number + " " + projectinfo.Name;

            //if current project not found. Prompt user.
            TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("PC_DIA002_Title")); //Project not found
            td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("PC_DIA002_MainInstr"), m_projectName); //Project Commander could not find the project: "{0}" in the XML database.
            td.ExpandedContent = LocalizationProvider.GetLocalizedValue<string>("PC_DIA002_ExpCont"); //Either this is the first time you have run the Project Commander for this particular project or the project has been renamed or renumbered since you last ran Project Commander.";
            td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("PC_DIA002_ComLink1")); //Create a new project entry.");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("PC_DIA002_ComLink2")); //Select from existing project in database.");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, LocalizationProvider.GetLocalizedValue<string>("PC_DIA002_ComLink3")); //Cancel and exit with no changes.");
            TaskDialogResult tdr = td.Show();

            //Create new project
            if (tdr == TaskDialogResult.CommandLink1)
            {
                m_DataSet = new Project_Commander.ProjectCommander();
                Project_Commander.ProjectCommander.ProjectRow row = m_DataSet.Project.NewRow() as Project_Commander.ProjectCommander.ProjectRow;
                row["ProjectID"] = pkhCommon.StringHelper.SafeForXML(m_projectName);
                row["UserField1"] = LocalizationProvider.GetLocalizedValue<string>("MT_cbi_none"); // -none-
                row["UserField2"] = LocalizationProvider.GetLocalizedValue<string>("MT_cbi_none"); // -none-
                m_DataSet.Project.AddProjectRow(row);
                m_DataSet.AcceptChanges();
                m_DataSet.WriteXml(XMLfilepath);
                return true;
            }

            //Select an existing project and renumber\rename it
            if (tdr == TaskDialogResult.CommandLink2)
            {
                Project_Commander.Project_Manager_Window pmw = new Project_Commander.Project_Manager_Window(XMLfilepath, m_projectName);
                IntPtr handle = commandData.Application.MainWindowHandle;
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(pmw);
                wih.Owner = handle;
                bool? result = pmw.ShowDialog();

                if (result != null && result == false)
                    return false;
                else
                    return true;
            }

            return false;
        }
    }

    public class pc_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            return !appdata.ActiveUIDocument.Document.IsFamilyDocument;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class project_manager : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(project_manager));

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /*
             * 
             * Opens the Project Manager form in delete mode.
             * 
             */
            try
            {
                //if XML does not exist, create a new one
                string XMLfilepath = Constants.RevvedBasePath + "ProjectCommander.xml";
                if (System.IO.File.Exists(XMLfilepath))
                {
                    Project_Commander.Project_Manager_Window pmw = new Project_Commander.Project_Manager_Window(XMLfilepath);
                    System.Windows.Interop.WindowInteropHelper x = new System.Windows.Interop.WindowInteropHelper(pmw);
                    x.Owner = commandData.Application.MainWindowHandle;
                    pmw.ShowDialog();
                }
                else
                {
                    TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("PC_DIA001_Title"), //"File not found."
                        LocalizationProvider.GetLocalizedValue<string>("PC_DIA001_Mess")); //"The database file could not be found. Have you run the Project Commander tool at least once yet?
                }
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("MT_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());

                return Result.Failed;
            }

            return Result.Cancelled;
        }
    }
}