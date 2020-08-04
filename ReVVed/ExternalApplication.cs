using System;
using System.IO;
using System.Windows.Media.Imaging;
using System.Net;
using System.Xml;

using Autodesk;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Events;
using log4net;
using log4net.Config;



namespace RVVD
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Journaling(Autodesk.Revit.Attributes.JournalingMode.NoCommandData)]
    public partial class ReVVed : IExternalApplication
    {
        global::Project_Commander.Docking_Pane m_MyDockableWindow = null;
        private static readonly ILog log = LogManager.GetLogger(typeof(ReVVed));

    //    private void CheckForUpdates()
    //    {
    //        try
    //        {
    //            if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
    //                return;

    //            if (Properties.Settings.Default.LastUpdateCheck.AddDays(7).CompareTo(System.DateTime.Today) >= 0)
    //                return;

				//log.Info("Checking for newer version.");
    //            Properties.Settings.Default.LastUpdateCheck = System.DateTime.Today;
    //            Properties.Settings.Default.Save();

    //            HttpWebRequest myHttpWebRequest = (HttpWebRequest)WebRequest.Create("http://www.pkhlineworks.ca/softwareversions.xml");
    //            HttpWebResponse myHttpWebResponse = (HttpWebResponse)myHttpWebRequest.GetResponse();
    //            // Gets the stream associated with the response.
    //            Stream receiveStream = myHttpWebResponse.GetResponseStream();

    //            XmlReader Xread = XmlReader.Create(receiveStream);
    //            Xread.ReadToFollowing("ReVVed" + Properties.Settings.Default.RevitVersion);
    //            int ver = Xread.ReadElementContentAsInt();
    //            int thisVer = Convert.ToInt32(Properties.Settings.Default.ReVVedVersion);

    //            if (ver > thisVer)
    //            {
				//	log.Info("A newer version is available.");
    //                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("EA_DIA001_Title")); //Update available
    //                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("EA_DIA001_MainInstr"); //"A newer version of ReVVed is available.";
    //                td.MainContent = string.Format(LocalizationProvider.GetLocalizedValue<string>("EA_DIA001_MainCont"), ver.ToString()); //Version {0} is available at <a href="http://www.pkhlineworks.ca">www.pkhlineworks.ca</a>
    //                TaskDialogResult tdr = td.Show();
    //            }

    //            // Releases the resources of the response.
    //            myHttpWebResponse.Close();
    //            // Releases the resources of the Stream.
    //            receiveStream.Close();
    //        }
    //        catch (Exception err)
    //        {
    //            log.Error("Could not check for updates.", err);
    //        }
    //    }

        private void CreateRibbonPanel(UIControlledApplication application)
        {
            // This method is used to create the ribbon panel.
            // which contains the controlled application.

            string AddinPath = Properties.Settings.Default.AddinPath;
            string DLLPath = AddinPath + @"\ReVVed.dll";

            Chelp.initializeHelp();

            // create a Ribbon panel.
            RibbonPanel revvedPanel = application.CreateRibbonPanel("ReVVed");

            // Create a Button to Show Docking Pane
            PushButtonData dpShow_Button = new PushButtonData("dpShowButton", LocalizationProvider.GetLocalizedValue<string>("EA_RButt_ShowDock"), DLLPath, "RVVD.ShowDockableWindow");
            dpShow_Button.Image = NewBitmapImage(this.GetType().Assembly, "componentcommander16x16.png");
            dpShow_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "componentcommander.png");
            dpShow_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("EA_RButt_ShowDock_Tooltip"); //Displays the Project Commander Dockable Palette
            dpShow_Button.AvailabilityClassName = "RVVD.pc_AvailableCheck";
            //TODO: dpShow_Button.SetContextualHelp(Chelp.coco_help);

            // Create a Button to Hide Docking Pane
            PushButtonData dpHide_Button = new PushButtonData("dpHideButton", LocalizationProvider.GetLocalizedValue<string>("EA_RButt_HideDock"), DLLPath, "RVVD.HideDockableWindow");
            dpHide_Button.Image = NewBitmapImage(this.GetType().Assembly, "componentcommander16x16.png");
            dpHide_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "componentcommander.png");
            dpHide_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("EA_RButt_HideDock_Tooltip"); //Hides the Project Commander Dockable Palette
            dpHide_Button.AvailabilityClassName = "RVVD.pc_AvailableCheck";

            System.Collections.Generic.IList<RibbonItem> pclist = revvedPanel.AddStackedItems(dpShow_Button, dpHide_Button);
            foreach (RibbonItem ri in pclist)
                ri.Visible = Properties.Settings.Default.ProjectCommander;

            //Create a Button for Project Commander
            PushButton pc_Button = revvedPanel.AddItem(new PushButtonData("pcButton", LocalizationProvider.GetLocalizedValue<string>("PC_Title").Replace(" ", "\n"), DLLPath, "RVVD.project_commander")) as PushButton;
            pc_Button.Image = NewBitmapImage(this.GetType().Assembly, "projectcommander16x16.png");
            pc_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "projectcommander.png");
            pc_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("PC_Tooltip"); //A small app for storing notes and details about the currently active project. These are not saved inside the project file.
            pc_Button.Visible = Properties.Settings.Default.ProjectCommander;
            pc_Button.AvailabilityClassName = "RVVD.pc_AvailableCheck";
            pc_Button.SetContextualHelp(Chelp.pc_help);

            revvedPanel.AddSeparator();

            // Create a Button for Component Commander 2.0
            PushButton coco2_Button = revvedPanel.AddItem(new PushButtonData("coco2Button", "COCO2", DLLPath, "RVVD.component_commander2")) as PushButton;
            coco2_Button.Image = NewBitmapImage(this.GetType().Assembly, "componentcommander16x16.png");
            coco2_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "componentcommander.png");
            coco2_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("CoCo_Tooltip"); //An improved interface for sorting and searching through components loaded into the project.
            coco2_Button.Visible = Properties.Settings.Default.ComponentCommander;
            coco2_Button.AvailabilityClassName = "RVVD.coco2_AvailableCheck";
            coco2_Button.SetContextualHelp(Chelp.coco_help);

            // Create a Button for Open Folder
            PushButton of_Button = revvedPanel.AddItem(new PushButtonData("ofButton", LocalizationProvider.GetLocalizedValue<string>("OF_Title"), DLLPath, "RVVD.open_folder")) as PushButton;
            of_Button.Image = NewBitmapImage(this.GetType().Assembly, "openfolder16x16.png");
            of_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "openfolder.png");
            of_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("OF_Tooltip"); //Opens the folder that the active project or selected family is saved in.
            of_Button.Visible = Properties.Settings.Default.OpenFolder;
            of_Button.AvailabilityClassName = "RVVD.of_AvailableCheck";
            of_Button.SetContextualHelp(Chelp.of_help);

            // Create a Button for merge text
            PushButton mt_Button = revvedPanel.AddItem(new PushButtonData("mtButton", LocalizationProvider.GetLocalizedValue<string>("MT_Title"), DLLPath, "RVVD.merge_text")) as PushButton;
            mt_Button.Image = NewBitmapImage(this.GetType().Assembly, "mergetext16x16.png");
            mt_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "mergetext.png");
            mt_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("MT_Tooltip"); //Merges multiple text notes into a single note.
            mt_Button.Visible = Properties.Settings.Default.MergeText;
            mt_Button.AvailabilityClassName = "RVVD.mt_AvailableCheck";
            mt_Button.SetContextualHelp(Chelp.mt_help);

            // Create a Button for poly line
            PushButton pl_Button = revvedPanel.AddItem(new PushButtonData("plButton", LocalizationProvider.GetLocalizedValue<string>("P_Title"), DLLPath, "RVVD.poly_line")) as PushButton;
            pl_Button.Image = NewBitmapImage(this.GetType().Assembly, "polyline16x16.png");
            pl_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "polyline.png");
            pl_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("P_Tooltip"); //Sets the total length of multiple joined lines.
            pl_Button.Visible = Properties.Settings.Default.PolyLine;
            pl_Button.AvailabilityClassName = "RVVD.pl_AvailableCheck";
            pl_Button.SetContextualHelp(Chelp.pl_help);

            // Create a Button for web link
            PushButton wl_Button = revvedPanel.AddItem(new PushButtonData("wlButton", LocalizationProvider.GetLocalizedValue<string>("WL_Title"), DLLPath, "RVVD.web_link")) as PushButton;
            wl_Button.Image = NewBitmapImage(this.GetType().Assembly, "weblink16x16.png");
            wl_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "weblink.png");
            wl_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("WL_Tooltip"); //Gets the URL of a component and opens the webpage in a minibrowser.
            wl_Button.Visible = Properties.Settings.Default.WebLink;
            wl_Button.AvailabilityClassName = "RVVD.wl_AvailableCheck";
            wl_Button.SetContextualHelp(Chelp.wl_help);

            // Create a Button for change case
            PushButton cc_Button = revvedPanel.AddItem(new PushButtonData("ccButton", LocalizationProvider.GetLocalizedValue<string>("CC_CommandName"), DLLPath, "RVVD.change_case")) as PushButton;
            cc_Button.Image = NewBitmapImage(this.GetType().Assembly, "changecase16x16.png");
            cc_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "changecase.png");
            cc_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("CC_Tooltip"); //Changes the case of selected items.
            cc_Button.Visible = Properties.Settings.Default.ChangeCase;
            cc_Button.AvailabilityClassName = "RVVD.cc_AvailableCheck";
            cc_Button.SetContextualHelp(Chelp.cc_help);

            //Create a Button for upper case
            PushButton uc_Button = revvedPanel.AddItem(new PushButtonData("ucButton", LocalizationProvider.GetLocalizedValue<string>("UC_Title"), DLLPath, "RVVD.upper_case")) as PushButton;
            uc_Button.Image = NewBitmapImage(this.GetType().Assembly, "uppercase16x16.png");
            uc_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "uppercase.png");
            uc_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("UC_Tooltip"); // Changes the case of selected items to upper case.
            uc_Button.Visible = Properties.Settings.Default.UpperCase;
            uc_Button.AvailabilityClassName = "RVVD.cc_AvailableCheck"; //upper case and change case can use same
            uc_Button.SetContextualHelp(Chelp.cc_help); //same as change case

            //Create a Button for grid flip
            PushButton gf_Button = revvedPanel.AddItem(new PushButtonData("gfButton", LocalizationProvider.GetLocalizedValue<string>("GF_Title"), DLLPath, "RVVD.grid_flip")) as PushButton;
            gf_Button.Image = NewBitmapImage(this.GetType().Assembly, "gridflip16x16.png");
            gf_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "gridflip.png");
            gf_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("GF_Tooltip"); //Flips the start and end points of grid lines.
            gf_Button.Visible = Properties.Settings.Default.GridFlip;
            gf_Button.AvailabilityClassName = "RVVD.projectopen_AvailableCheck";
            gf_Button.SetContextualHelp(Chelp.gf_help);

            //Create a Button for revisionist
            PushButton rev_Button = revvedPanel.AddItem(new PushButtonData("revButton", LocalizationProvider.GetLocalizedValue<string>("R_Title"), DLLPath, "RVVD.revisionist")) as PushButton;
            rev_Button.Image = NewBitmapImage(this.GetType().Assembly, "revisionist16x16.png");
            rev_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "revisionist.png");
            rev_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("R_Title"); //Adds revisions to multiple sheets.
            rev_Button.Visible = Properties.Settings.Default.Revisionist;
            rev_Button.AvailabilityClassName = "RVVD.projectopen_AvailableCheck";
            rev_Button.SetContextualHelp(Chelp.rev_help);

            // Create a slide out and button for Options and Project Manager
            revvedPanel.AddSlideOut();

            PushButtonData hlp_Button = new PushButtonData("hlpButton", LocalizationProvider.GetLocalizedValue<string>("HelpButton"), DLLPath, "RVVD.revved_help");
            hlp_Button.Image = NewBitmapImage(this.GetType().Assembly, "help16x16.png");
            hlp_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "help.png");
            hlp_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("HLP_Tooltip"); //Help for the ReVVed suite of tools.
            hlp_Button.AvailabilityClassName = "RVVD.all_AvailableCheck";
            hlp_Button.SetContextualHelp(Chelp.help_help);

            PushButtonData about_Button = new PushButtonData("aboutButton", LocalizationProvider.GetLocalizedValue<string>("About_Title"), DLLPath, "RVVD.revved_about");
            about_Button.Image = NewBitmapImage(this.GetType().Assembly, "about16x16.png");
            about_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "about.png");
            about_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("About_Tooltip"); //Product information about the ReVVed suite of tools.
            about_Button.AvailabilityClassName = "RVVD.all_AvailableCheck";

            PushButtonData opt_Button = new PushButtonData("optButton", LocalizationProvider.GetLocalizedValue<string>("OPT_Title"), DLLPath, "RVVD.options");
            opt_Button.Image = NewBitmapImage(this.GetType().Assembly, "setting16x16.png");
            opt_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "setting.png");
            opt_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("OPT_Tooltip"); //Configuration options for the ReVVed suite of tools.
            opt_Button.AvailabilityClassName = "RVVD.all_AvailableCheck";
            opt_Button.SetContextualHelp(Chelp.opt_help);

            PushButtonData pm_Button = new PushButtonData("pmButton", LocalizationProvider.GetLocalizedValue<string>("PC_PMW_Title"), DLLPath, "RVVD.project_manager");
            pm_Button.Image = NewBitmapImage(this.GetType().Assembly, "projectmanager16x16.png");
            pm_Button.LargeImage = NewBitmapImage(this.GetType().Assembly, "projectmanager.png");
            pm_Button.ToolTip = LocalizationProvider.GetLocalizedValue<string>("PC_PMW_Tooltip"); //Allows you to delete obsolete projects from the Project Commander database.
            pm_Button.AvailabilityClassName = "RVVD.all_AvailableCheck";
            pm_Button.SetContextualHelp(Chelp.pc_help); //same as project commander

            revvedPanel.AddStackedItems(opt_Button, pm_Button);
            revvedPanel.AddStackedItems(hlp_Button, about_Button);
        }

        internal void LogEvent(string evnt)
        {
            if (!Properties.Settings.Default.LogToFile)
                return;

            string text = null;
            string logpath = null;
            text = DateTime.Now.ToString("dd MMM HH:mm:ss");
            text += "\t" + evnt + "\r\n";

            if (Properties.Settings.Default.LogFilePath == "none")
                logpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            else
                logpath = Properties.Settings.Default.LogFilePath;
            string logfile = System.IO.Path.Combine(logpath, "ReVVed.log");

            try
            {
                File.AppendAllText(logfile, text, System.Text.Encoding.UTF8);
            }
            catch (IOException err)
            {
                log.Error("Could not write to event logger.", err);
                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("EA_DIA002_Title")); //Logging Error
                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("EA_DIA002_MainInstr"), logfile); //ReVVed could not access the log file: {0}. It is probably locked by another process.
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("EA_DIA002_MainCont"); //Would you like to turn off logging or just ignore this error and continue.
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("EA_DIA002_Comm1")); //Turn off logging.
                td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("EA_DIA002_Comm2")); //Ignore and continue.
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                if (tdr == TaskDialogResult.CommandLink1)
                {
                    Properties.Settings.Default.LogToFile = false;
                    Properties.Settings.Default.Save();
                }
            }
        }

        internal void TrimLogFile()
        {
            string logpath = null;

            if (Properties.Settings.Default.LogToFile == false)
                return;
            if (Properties.Settings.Default.LogDuration == 0)
                return;

            if (Properties.Settings.Default.LogFilePath == "none")
                logpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            else
                logpath = Properties.Settings.Default.LogFilePath;
            string logfile = System.IO.Path.Combine(logpath, "ReVVed.log");

            System.Collections.Generic.List<string> logentries = new System.Collections.Generic.List<string>(File.ReadAllLines(logfile));

            while (logentries.Count != 0)
            {
                try
                {
                    string s = logentries[0].Substring(0, 15);
                    if (s.Contains("Logging level"))
                    {
                        logentries.RemoveAt(0);
                    }
                    else
                    {
                        DateTime logdate = DateTime.ParseExact(s, "dd MMM HH:mm:ss", null);
                        DateTime cutoffdate = DateTime.Now.AddDays((int)Properties.Settings.Default.LogDuration * -1);
                        if (DateTime.Compare(logdate, cutoffdate) < 0 || DateTime.Compare(logdate, DateTime.Now) > 0)
                            logentries.RemoveAt(0);
                        else
                            break;
                    }
                }
                catch (Exception err)
                {
                    FormatException fe = err as FormatException;
                    if (fe != null)
                    {
						log.Warn("Found a malformed log event entry.");
                        logentries.RemoveAt(0);
                        continue;
                    }
                    else
                        throw;
                }
            }

            File.WriteAllLines(logfile, logentries.ToArray());
			log.Info("Trimmed event log file.");
        }

        /// <summary>
        /// Load a new icon bitmap from embedded resources.
        /// For the BitmapImage, make sure you reference WindowsBase and PresentationCore, and import the System.Windows.Media.Imaging namespace.
        /// Drag images into Resources folder in solution explorer and set build action to "Embedded Resource"
        /// </summary>
        private BitmapImage NewBitmapImage(System.Reflection.Assembly a, string imageName)
        {
            Stream s = a.GetManifestResourceStream("RVVD.Resources.RibbonImages." + imageName);
            BitmapImage img = new BitmapImage();

            img.BeginInit();
            img.StreamSource = s;
            img.EndInit();

            return img;
        }

        public static DockablePaneId getProjCommanderDockPaneId()
        {
            return new DockablePaneId(new Guid("{D7C963CE-B7CA-426A-8D51-6E8254D21157}"));
        }

        private void registerDockableWindow(UIControlledApplication application)
        {
            DockablePaneId dpid = getProjCommanderDockPaneId();
            if (DockablePane.PaneIsRegistered(dpid))
                return;

            global::Project_Commander.Docking_Pane MainDockableWindow = new global::Project_Commander.Docking_Pane();
            m_MyDockableWindow = MainDockableWindow;

            DockablePaneProviderData data = new DockablePaneProviderData();
            data.FrameworkElement = m_MyDockableWindow as System.Windows.FrameworkElement;
            data.InitialState = new DockablePaneState();
            data.InitialState.DockPosition = DockPosition.Tabbed;
            data.InitialState.TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser;

            application.RegisterDockablePane(dpid, LocalizationProvider.GetLocalizedValue<string>("PC_Title"), m_MyDockableWindow as IDockablePaneProvider);

            application.ViewActivated += new EventHandler<ViewActivatedEventArgs>(DockablePane_ViewActivated);
        }

        public Autodesk.Revit.UI.Result OnShutdown(UIControlledApplication application)
        {
            LogEvent("Revit shutting down.");
            DeregisterLoggerEventHandlers(application);
            application.ViewActivated -= DockablePane_ViewActivated;
            application.ControlledApplication.DocumentOpened -= ProjectCommander_Doc_Opened;
            return Autodesk.Revit.UI.Result.Succeeded;
        }

        public Autodesk.Revit.UI.Result OnStartup(UIControlledApplication application)
        {
            try
            {
                System.Reflection.Assembly a = this.GetType().Assembly;
                Properties.Settings.Default.RevitVersion = a.GetName().Version.Major.ToString();
                Properties.Settings.Default.ReVVedVersion = a.GetName().Version.Minor.ToString();

                // setup addin path property setting for global access
                string s = a.Location;
                int x = s.IndexOf(@"\reVVed.dll", StringComparison.CurrentCultureIgnoreCase);
                s = s.Substring(0, x);
                Properties.Settings.Default.AddinPath = s;

                var logConfig = Path.Combine(Properties.Settings.Default.AddinPath, "revved.log4net.config");
#if DEBUG
                log4net.Util.LogLog.InternalDebugging = true;
#endif
                var configStream = new FileInfo(logConfig);
                XmlConfigurator.Configure(configStream);
                log.InfoFormat("Running version: {0}", a.GetName().Version.ToString());
                log.InfoFormat("Found myself at: {0}", Properties.Settings.Default.AddinPath);
                // setup event handlers for Logger.cs
                RegisterLoggerEventHandlers(application);
                LogEvent("Revit started.\r\nLogging level set to: " + Properties.Settings.Default.LogLevel.ToString());

                application.ControlledApplication.DocumentOpened += ProjectCommander_Doc_Opened;
                registerDockableWindow(application);

                CreateRibbonPanel(application);
                TrimLogFile();
                //CheckForUpdates();
                //CheckAnalytics();
                return Autodesk.Revit.UI.Result.Succeeded;
            }
            catch (Exception err)
            {
				log.Error("Could not startup app", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = "ReVVed" + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(application.ControlledApplication.VersionName, application.ControlledApplication.VersionBuild, err, this.GetType().Assembly.GetName());

                return Result.Failed;
            }
        }

    //    private void CheckAnalytics()
    //    {
    //        //indicates a first run therefore ask permision
    //        if(Properties.Settings.Default.AnonUId == System.Guid.Empty && Properties.Settings.Default.AllowAnalytics == true)
    //        {
				//log.Info("Requesting permission to track analytics");
    //            Properties.Settings.Default.AnonUId = System.Guid.NewGuid();

    //            TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("EA_DIA003_Title")); //ReVVed User Experience
    //            td.MainContent = LocalizationProvider.GetLocalizedValue<string>("EA_DIA003_MainInstr"); //pkh Lineworks would like to collect information on how you use ReVVed.
    //            td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("EA_DIA003_MainCont"); //This would happen automatically without you needing to anything and requires an internet connection. If you want to opt out at any time you can do so in the Options menu.
    //            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("EA_DIA003_Comm1")); //Yes, I would like to contribute.
    //            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("EA_DIA003_Comm2")); //No, I don't want to help make ReVVed better.
    //            td.FooterText = LocalizationProvider.GetLocalizedValue<string>("EA_DIA003_Footer"); //<a href="http://www.pkhlineworks.ca/analyticsinformation.html"> Click here for more information</a>
    //            TaskDialogResult tdr = td.Show();
    //            if (tdr == TaskDialogResult.CommandLink1)
    //                Properties.Settings.Default.AllowAnalytics = true;
    //            Properties.Settings.Default.Save();
    //        }
    //    }

#region Log Event Handlers
        internal void RegisterLoggerEventHandlers(Autodesk.Revit.UI.UIControlledApplication application)
        {
            //LogLevel = 2
            application.ControlledApplication.DocumentChanged += new EventHandler<DocumentChangedEventArgs>(App_Doc_Changed);
            application.ViewActivated += new EventHandler<ViewActivatedEventArgs>(App_View_Activated);

            //LogLevel = 1
            application.ControlledApplication.DocumentSynchronizedWithCentral += new EventHandler<DocumentSynchronizedWithCentralEventArgs>(App_Doc_Synched);
            application.ControlledApplication.DocumentSaved += new EventHandler<DocumentSavedEventArgs>(App_Doc_Saved);
            application.ControlledApplication.DocumentPrinted += new EventHandler<DocumentPrintedEventArgs>(App_Doc_Printed);

            //LogLevel = 0
            application.ControlledApplication.DocumentCreated += new EventHandler<DocumentCreatedEventArgs>(App_Doc_Created);
            application.ControlledApplication.DocumentClosing += new EventHandler<DocumentClosingEventArgs>(App_Doc_Closing);
            application.ControlledApplication.DocumentOpened += new EventHandler<DocumentOpenedEventArgs>(App_Doc_Opened);
        }

        internal void DeregisterLoggerEventHandlers(Autodesk.Revit.UI.UIControlledApplication application)
        {
            application.ViewActivated -= App_View_Activated;
            application.ControlledApplication.DocumentSynchronizedWithCentral -= App_Doc_Synched;
            application.ControlledApplication.DocumentSaved -= App_Doc_Saved;
            application.ControlledApplication.DocumentPrinted -= App_Doc_Printed;
            application.ControlledApplication.DocumentCreated -= App_Doc_Created;
            application.ControlledApplication.DocumentChanged -= App_Doc_Changed;
            application.ControlledApplication.DocumentClosing -= App_Doc_Closing;
            application.ControlledApplication.DocumentOpened -= App_Doc_Opened;
        }

        public void App_View_Activated(object sender, ViewActivatedEventArgs args)
        {
            if (Properties.Settings.Default.LogLevel < 2)
                return;

            string str = "Activated view: ";
            str += args.CurrentActiveView.Name;
            str += " in document ";
            str += args.Document.PathName;
            LogEvent(str);
        }

        public void App_Doc_Synched(object sender, DocumentSynchronizedWithCentralEventArgs args)
        {
            if (Properties.Settings.Default.LogLevel < 1)
                return;

            string str = "Synchronized " + args.Document.PathName + " with central.";
            LogEvent(str);
        }

        public void App_Doc_Saved(object sender, DocumentSavedEventArgs args)
        {
            if (Properties.Settings.Default.LogLevel < 1)
                return;

            LogEvent("Saved file: " + args.Document.PathName);
        }

        public void App_Doc_Printed(object sender, DocumentPrintedEventArgs args)
        {
            if (Properties.Settings.Default.LogLevel < 1)
                return;
            foreach (Autodesk.Revit.DB.ElementId e in args.GetPrintedViewElementIds())
            {
                View v = args.Document.GetElement(e) as View;
                LogEvent("Printed view: " + v.Name + ".");
            }
        }

        public void App_Doc_Created(object sender, DocumentCreatedEventArgs args)
        {
            LogEvent("Created new document.");
        }

        public void App_Doc_Changed(object sender, DocumentChangedEventArgs args)
        {
            if (Properties.Settings.Default.LogLevel < 2)
                return;

            System.Collections.Generic.IList<string> transList = args.GetTransactionNames();
            foreach (string command in transList)
            {
                string str = null;

                switch (args.Operation)
                {
                    case UndoOperation.TransactionUndone:
                        str += "Undo command: ";
                        break;
                    case UndoOperation.TransactionRedone:
                        str += "Redo command: ";
                        break;
                    default:
                        str += "Execute command: ";
                        break;
                }

                str += command + ".";
                LogEvent(str);
            }
        }

        public void App_Doc_Closing(object sender, DocumentClosingEventArgs args)
        {
            /*
             *
             * Before a document has been closed.
             *
             */
            LogEvent("Closing file: " + args.Document.PathName);
        }

        public void App_Doc_Opened(object sender, DocumentOpenedEventArgs args)
        {
            /*
             *
             * After a document has been opened.
             * This is used for Logging Events and running Project Commander at startup
             *
             */
            LogEvent("Opened file: " + args.Document.PathName);
        }
#endregion

#region Project Commander Event Handlers
        public void ProjectCommander_Doc_Opened(object sender, DocumentOpenedEventArgs args)
        {
            //run project commander but it should exit quitely if current project is not in XML
            if (Properties.Settings.Default.PC_runatstart)  // if user wants to check at startup
            {
                Document actdoc = args.Document;
                if (!actdoc.IsFamilyDocument) // if it is not a family document
                {
                    string XMLfilepath = RVVD.Constants.RevvedBasePath + "ProjectCommander.xml";
                    if (System.IO.File.Exists(XMLfilepath)) // if XML database exists. Not first run.
                    {
                        //extract project info from Revit project file.
                        //Transaction transaction = new Transaction(actdoc);
                        //transaction.Start("ReVVed Project Commander");

                        ProjectInfo pi = actdoc.ProjectInformation;
                        try
                        {
                            Project_Commander.Project_Command_Window pcw = new Project_Commander.Project_Command_Window(XMLfilepath);
                            UIApplication uiapp = sender as UIApplication;
                            IntPtr handle = uiapp.MainWindowHandle;
                            System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(pcw);
                            wih.Owner = handle;
                            if (pcw.setProject(pi))
                                pcw.ShowDialog();
                        }
                        catch (Exception err)
                        {
							log.Error("Unexpected exception", err);

                            Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                            td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("PC_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                            td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                            td.ExpandedContent = err.ToString();
                            //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                            td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                            TaskDialogResult tdr = td.Show();

                            //if (tdr == TaskDialogResult.CommandLink1)
                            //    pkhCommon.Email.SendErrorMessage(args.Document.Application.VersionName, args.Document.Application.VersionBuild, err, this.GetType().Assembly.GetName());
                        }

                        //transaction.RollBack();
                    }
                }
            }
        }

        void DockablePane_ViewActivated(object sender, ViewActivatedEventArgs e)
        {
            try
            {
                if (m_MyDockableWindow != null && m_MyDockableWindow.IsInitialized)
                {
                    ProjectInfo pi = e.Document.ProjectInformation;
                    if (pi != null)
                        m_MyDockableWindow.UpdateProject(pi.Number + " " + pi.Name);
                    else
                        m_MyDockableWindow.UpdateProject(null);
                }
            }
            catch (Exception err)
            {
				log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("PC_Doc_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(e.Document.Application.VersionName, e.Document.Application.VersionBuild, err, this.GetType().Assembly.GetName());
            }
        }
#endregion
    }
}