using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Debug = System.Diagnostics.Debug;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RVVD
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class revisionist : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(revisionist));
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document actDoc = commandData.Application.ActiveUIDocument.Document;
            Transaction trans = null;
            Revision_Window rw = null;
            try
            {
                //using (Measurement_Protocol mp = new Measurement_Protocol())
                //{
                //    mp.sendAnalytic(Constants.REVISIONIST_MACRO_NAME, commandData);
                //}

                //get revisions
                IList<ElementId> revisionIds = Revision.GetAllRevisionIds(actDoc);
                IEnumerator<ElementId> itor = revisionIds.GetEnumerator();

                Revision[] revisions = new Revision[revisionIds.Count];
                int x = 0;
                itor.Reset();
                while (itor.MoveNext())
                {
                    Revision rev = actDoc.GetElement(itor.Current as ElementId) as Revision;
                    if (!rev.Issued)
                    {
                        revisions[x] = rev;
                        x++;
                    }
                }

                if (x == 0) //no un-issued revisions
                {
                    log.InfoFormat("{0} found no data. All revisions have been issued.", Constants.REVISIONIST_MACRO_NAME);
                    TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("R_DIA001_Title"), //No data
                        LocalizationProvider.GetLocalizedValue<string>("R_DIA001_MainInstr")); //All the revsions have been issued.
                    return Result.Cancelled;
                }

                //get sheets
                FilteredElementCollector collector2 = new FilteredElementCollector(actDoc);
                collector2.OfClass(typeof(ViewSheet));
                FilteredElementIterator fitor = collector2.GetElementIterator();

                ObservableCollection<ViewSheetInformation> sheetCollection = new ObservableCollection<ViewSheetInformation>();

                fitor.Reset();
                while (fitor.MoveNext())
                {
                    ViewSheet rfa = fitor.Current as ViewSheet;
                    if (!rfa.IsPlaceholder)
                        sheetCollection.Add(new ViewSheetInformation(rfa));
                }

                //no sheets
                if (sheetCollection.Count < 1)
                {
                    log.InfoFormat("{0} found no data. There are no sheets to work with.", Constants.REVISIONIST_MACRO_NAME);
                    TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("R_DIA001_Title"), //No data
                        LocalizationProvider.GetLocalizedValue<string>("R_DIA002_MainInstr")); //There are no sheets to work with.
                    return Result.Cancelled;
                }

                rw = new Revision_Window(revisions, sheetCollection);
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(rw);
                wih.Owner = commandData.Application.MainWindowHandle;
                rw.ShowDialog();

                if ((bool)rw.DialogResult)
                {
                    sheetCollection = rw.theCollection;
                    ElementId theRevision = rw.selectedId;
                    trans = new Transaction(actDoc);
                    trans.Start(Constants.REVISIONIST_MACRO_NAME);
                    foreach (ViewSheetInformation vsi in sheetCollection)
                    {
                        if (vsi.selectedSheet)
                        {
                            ViewSheet vs = actDoc.GetElement(vsi.sheetId) as ViewSheet;
                            ICollection<ElementId> revs = vs.GetAdditionalRevisionIds();
                            revs.Add(theRevision);
                            vs.SetAdditionalRevisionIds(revs);
                        }
                    }
                    trans.Commit();
                    log.InfoFormat("{0} updated {1} sheets.", Constants.REVISIONIST_MACRO_NAME, sheetCollection.Count.ToString());
                    return Result.Succeeded;
                }
                return Result.Cancelled;
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("R_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());

                if (rw != null && rw.IsLoaded)
                    rw.Close();
                if (trans != null && trans.HasStarted())
                    trans.RollBack();
                return Result.Failed;
            }
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class grid_flip : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(grid_flip));
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Autodesk.Revit.DB.Document actdoc = commandData.Application.ActiveUIDocument.Document;
            Autodesk.Revit.UI.Selection.Selection selection = commandData.Application.ActiveUIDocument.Selection;
            Transaction tr = new Transaction(actdoc);
            try
            {
                //using (Measurement_Protocol mp = new Measurement_Protocol())
                //{
                //    mp.sendAnalytic(Constants.GRID_FLIP_MACRO_NAME, commandData);
                //}

                bool had_invalid_grids = false;
                int gridCount = 0;
                tr.Start(Constants.GRID_FLIP_MACRO_NAME);
                foreach (ElementId eid in selection.GetElementIds())
                {
                    Element el = actdoc.GetElement(eid);
                    if (el is Grid)
                    {
                        Grid grid = el as Grid;
                        if (grid != null)
                        {
                            if (!grid.IsCurved) //arc's cannot be reversed
                            {
                                Grid newGrid = null;
                                string label = grid.Name;
                                bool pinned_status = grid.Pinned;
                                ElementId t_id = grid.GetTypeId();
                                Curve newCurve = grid.Curve.CreateReversed();
                                actdoc.Delete(eid);
                                newGrid = Grid.Create(actdoc, newCurve as Line);
                                newGrid.ChangeTypeId(t_id);
                                newGrid.Name = label;
                                newGrid.Pinned = pinned_status;
                                gridCount++;
                            }
                            else
                            {
                                had_invalid_grids = true;
                            }
                        }
                    }
                    else if (el is MultiSegmentGrid)    // can't determine start endpoint. GetGridIds() returns unordered list.
                    {
                        had_invalid_grids = true;
                    }
                }
                tr.Commit();
                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("GF_Title"));
                td.MainInstruction = string.Format(LocalizationProvider.GetLocalizedValue<string>("GF_DIA001_MainInstr"), gridCount); //Flipped {0} grid lines.
                if (had_invalid_grids)
                    td.MainContent = LocalizationProvider.GetLocalizedValue<string>("GF_DIA001_MainCont");//There were arced or multi-segment gridlines selected. These cannot be flipped and were ignored.
                td.Show();
                return Result.Succeeded;
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("GF_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());
                if (tr != null && tr.HasStarted())
                    tr.RollBack();
                return Result.Failed;
            }
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class open_folder : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(open_folder));
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /*
             *  To open the windows explorer to:
             *      - add point cloud and DWF support ---------- TODO
             *      - the directory of a linked file or family if one is selected
             *      - the directory of the central file if worksets are enabled
             *      - the directory of the active project\family file otherwise
             */
            string directory = null;
            Autodesk.Revit.UI.Selection.Selection sel = commandData.Application.ActiveUIDocument.Selection;
            Autodesk.Revit.DB.Document actdoc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                //using (Measurement_Protocol mp = new Measurement_Protocol())
                //{
                //    mp.sendAnalytic(Constants.OPEN_FOLDER_MACRO_NAME, commandData);
                //}

                string error = LocalizationProvider.GetLocalizedValue<string>("OF_Error1"); //A proper directory could not be found. One possible reason is that this is a new project that has not been saved yet.

                if (sel.GetElementIds().Count == 1)
                {
                    System.Collections.IEnumerator es = sel.GetElementIds().GetEnumerator();
                    es.MoveNext();

                    Type T = actdoc.GetElement((ElementId)es.Current).GetType();
                    if (T == typeof(ImportInstance))
                    {
                        ImportInstance imp = actdoc.GetElement((ElementId)es.Current) as ImportInstance;
                        if (imp.IsLinked)
                        {
                            ExternalFileReference el = ExternalFileUtils.GetExternalFileReference(actdoc, imp.GetTypeId());
                            if (el != null)
                                directory = ModelPathUtils.ConvertModelPathToUserVisiblePath(el.GetAbsolutePath());
                        }
                        else
                            error = LocalizationProvider.GetLocalizedValue<string>("OF_Error2"); //The import instance you selected is not linked into the project.
                    }
                    else if (T == typeof(Instance))
                    {
                        Element e = actdoc.GetElement((ElementId)es.Current) as Element;
                        if ((BuiltInCategory)e.Category.Id.IntegerValue == BuiltInCategory.OST_RvtLinks)
                        {
                            Instance rlt = es.Current as Instance;
                            ExternalFileReference el = ExternalFileUtils.GetExternalFileReference(actdoc, rlt.GetTypeId());
                            if (el != null)
                                directory = ModelPathUtils.ConvertModelPathToUserVisiblePath(el.GetAbsolutePath());
                        }
                    }
                    else if (T == typeof(FamilyInstance))
                    {
                        FamilyInstance fi = actdoc.GetElement((ElementId)es.Current) as FamilyInstance;
                        Family fam = fi.Symbol.Family;
                        if (fam.IsInPlace)
                            error = LocalizationProvider.GetLocalizedValue<string>("OF_Error4"); //Selected family was an in-place family.
                        else
                        {
                            Document famdoc = actdoc.EditFamily(fam);
                            directory = famdoc.PathName;
                            famdoc.Close(false);
                            if (directory == null || directory == string.Empty)
                            {
                                error = LocalizationProvider.GetLocalizedValue<string>("OF_Error5"); //The selected family had no directory path. One possible reason is the family was not loaded into the project but existed in the project template.
                                directory = null;
                            }
                        }
                    }
                    else if (T == typeof(PointCloudInstance))
                    {
                        error = LocalizationProvider.GetLocalizedValue<string>("OF_Error6"); //Point Clouds are not supported at this time.
                    }
                    else
                    {
                        error = LocalizationProvider.GetLocalizedValue<string>("OF_Error7"); //The type of element selected could not be recognized as an external reference.
                    }
                }
                else //when no elements are selected open path to project file
                {
                    if (actdoc.IsWorkshared)
                        directory = actdoc.GetWorksharingCentralModelPath().CentralServerPath;
                    else
                        directory = actdoc.PathName;
                }

                if (directory != null && directory != string.Empty && !System.IO.File.Exists(directory))
                {
                    error = LocalizationProvider.GetLocalizedValue<string>("OF_Error8"); //The directory {0} does not exist.
                    directory = null;
                }

                if (directory != null && directory != string.Empty)
                {
                    System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
                    myProcess.StartInfo.FileName = System.IO.Path.GetDirectoryName(directory);
                    myProcess.StartInfo.UseShellExecute = true;
                    myProcess.StartInfo.RedirectStandardOutput = false;
                    myProcess.Start();
                    myProcess.Dispose();
                    log.InfoFormat("{0} opened directory: {1}", Constants.OPEN_FOLDER_MACRO_NAME, directory);
                }
                else
                {
                    log.InfoFormat("{0} encountered an error: {1}", Constants.OPEN_FOLDER_MACRO_NAME, error);
                    TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("OF_Error_Title"), //Error
                        error);
                }
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("OF_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
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
    }

    public class of_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            if (appdata.ActiveUIDocument.Selection.GetElementIds().Count > 1)
                return false;
            return true;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class change_case : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(change_case));
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /*
             *
             * 1) check to see if items are selected (if no, return error or prompt for all text in current view)
             * 2) Prompt for style of case change (Eg. all caps, lowercase, sentance, etc)
             * 3) Cycle through all text and add items to error set if failed.
             *
             * IDEAS
             * 1) add a preview window
             *
             */
            System.Diagnostics.Debug.WriteLine("------- Case Change -------");
            Autodesk.Revit.DB.Document actdoc = commandData.Application.ActiveUIDocument.Document;
            ElementSet sel = new ElementSet();
            foreach (ElementId eid in commandData.Application.ActiveUIDocument.Selection.GetElementIds())
            {
                sel.Insert(commandData.Application.ActiveUIDocument.Document.GetElement(eid));
            }

            Transaction transaction = null;

            try
            {
                //using (Measurement_Protocol mp = new Measurement_Protocol())
                //{
                //    mp.sendAnalytic(Constants.CHANGE_CASE_MACRO_NAME, commandData);
                //}

                if (sel.IsEmpty)
                {
                    TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("CC_CommandName"),
                        LocalizationProvider.GetLocalizedValue<string>("CC_DIA001")); //You must select the text items first.
                    return Result.Cancelled;
                }

                Case_Chooser cc = new Case_Chooser();
                System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
                IntPtr handle = process.MainWindowHandle;
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(cc);
                wih.Owner = handle;

                if (cc.ShowDialog() == true)
                {
                    transaction = new Transaction(actdoc);
                    transaction.Start(LocalizationProvider.GetLocalizedValue<string>("CC_CommandName"));
                    int count = 0;
                    string choice = string.Empty;

                    //cycle through selection and change if text (add to error if not)
                    foreach (Element theItem in sel)
                    {
                        if (theItem.Category != null)
                        {
                            if ((BuiltInCategory)theItem.Category.Id.IntegerValue == BuiltInCategory.OST_TextNotes)
                            {
                                TextNote note = actdoc.GetElement(theItem.UniqueId) as TextNote;
                                FormattedText oldText = note.GetFormattedText();
                                string s = oldText.GetPlainText();
                                System.Diagnostics.Debug.WriteLine(s);
                                var textInfo = System.Globalization.CultureInfo.CurrentUICulture.TextInfo;
                                switch (cc.theMode)
                                {
                                    case Case_Chooser.CaseMode.Sentance:
                                        s = pkhCommon.StringHelper.SentenceCase(s);
                                        choice = LocalizationProvider.GetLocalizedValue<string>("MT_cbi_sentanceCase"); // "sentance case";
                                        break;
                                    case Case_Chooser.CaseMode.Lower:
                                        s = textInfo.ToLower(s);
                                        choice = LocalizationProvider.GetLocalizedValue<string>("MT_cbi_lowerCase"); //"lower case";
                                        break;
                                    case Case_Chooser.CaseMode.Upper:
                                        s = textInfo.ToUpper(s);
                                        choice = LocalizationProvider.GetLocalizedValue<string>("MT_cbi_UpperCase"); //"upper case";
                                        break;
                                    case Case_Chooser.CaseMode.Title:
                                        s = textInfo.ToLower(s);
                                        s = textInfo.ToTitleCase(s);
                                        choice = LocalizationProvider.GetLocalizedValue<string>("MT_cbi_titleCase"); //"title case";
                                        break;
                                    default: // toggle
                                        s = pkhCommon.StringHelper.ToggleCase(s);
                                        choice = LocalizationProvider.GetLocalizedValue<string>("MT_cbi_toggleCase"); //"toggle case";
                                        break;
                                }
                                FormattedText newText = new FormattedText(s);
                                for (TextRange tr = new TextRange(0, 1); tr.Start < oldText.AsTextRange().End; tr.Start++)
                                {
                                    newText.SetBoldStatus(tr, oldText.GetBoldStatus(tr) == FormatStatus.All);
                                    newText.SetItalicStatus(tr, oldText.GetItalicStatus(tr) == FormatStatus.All);
                                    newText.SetListType(tr, oldText.GetListType(tr));
                                    if (oldText.GetMinimumListStartNumber() < oldText.GetListStartNumber(tr) &&
                                        oldText.GetMaximumListStartNumber() > oldText.GetListStartNumber(tr) &&
                                        oldText.GetListType(tr) != ListType.Bullet &&
                                        oldText.GetListType(tr) != ListType.None &&
                                        oldText.GetIndentLevel(tr) == 1)
                                        newText.SetListStartNumber(tr, oldText.GetListStartNumber(tr));
                                    newText.SetSubscriptStatus(tr, oldText.GetSubscriptStatus(tr) == FormatStatus.All);
                                    newText.SetSuperscriptStatus(tr, oldText.GetSuperscriptStatus(tr) == FormatStatus.All);
                                    newText.SetUnderlineStatus(tr, oldText.GetUnderlineStatus(tr) == FormatStatus.All);
                                    newText.SetIndentLevel(tr, oldText.GetIndentLevel(tr)); //needs to be last. Formatting resets indent?
                                    Debug.WriteLine("Indent level: " + oldText.GetIndentLevel(tr));
                                }
                                note.SetFormattedText(newText);
                                ++count;
                            }
                        }
                    }
                    log.InfoFormat("{0} converted {1} text notes to {2}", LocalizationProvider.GetLocalizedValue<string>("CC_CommandName"), count.ToString(), choice);
                    transaction.Commit();
                }
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("CC_CommandName") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());
                if (transaction != null && transaction.HasStarted())
                    transaction.RollBack();
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }

    public class cc_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            if (selectCatagories.Size > 1)
                return false;
            foreach (Category c in selectCatagories)
            {
                if (c.Id.IntegerValue == (int)BuiltInCategory.OST_TextNotes)
                    return true;
            }

            return false;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class upper_case : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(upper_case));
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /*
             *
             * A quick version of change case. It just changes the text to upper case.
             *
             */
            Transaction transaction = null;
            Autodesk.Revit.DB.Document actdoc = commandData.Application.ActiveUIDocument.Document;
            ElementSet sel = new ElementSet();
            foreach (ElementId eid in commandData.Application.ActiveUIDocument.Selection.GetElementIds())
            {
                sel.Insert(commandData.Application.ActiveUIDocument.Document.GetElement(eid));
            }

            try
            {
                //using (Measurement_Protocol mp = new Measurement_Protocol())
                //{
                //    mp.sendAnalytic(Constants.UPPER_CASE_MACRO_NAME, commandData);
                //}

                if (sel.IsEmpty)
                {
                    return Result.Failed;
                }
                else
                {
                    transaction = new Transaction(actdoc);
                    transaction.Start(LocalizationProvider.GetLocalizedValue<string>("UC_Title"));
                    int count = 0;
                    //cycle through selection and change if text
                    foreach (Element theItem in sel)
                    {
                        if (theItem.Category != null)
                        {
                            if ((BuiltInCategory)theItem.Category.Id.IntegerValue == BuiltInCategory.OST_TextNotes)
                            {
                                TextNote note = actdoc.GetElement(theItem.UniqueId) as TextNote;
                                FormattedText ft = note.GetFormattedText();
                                ft.SetAllCapsStatus(true);
                                note.SetFormattedText(ft);
                                ++count;
                            }
                        }
                    }
                    transaction.Commit();
                    log.InfoFormat("{0} converted {1} text notes", Constants.UPPER_CASE_MACRO_NAME, count.ToString());
                }
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("UC_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());

                if (transaction != null)
                    transaction.RollBack();
                return Result.Failed;
            }
            return Result.Succeeded;
        }
    }

    //upper case uses same AvailableCheck as change case

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class revved_help : IExternalCommand
    {
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Chelp.help_help.Launch();
            return Result.Succeeded;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class options : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(options));

        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /*
             *
             * Opens the options form
             *
             */

            try
            {
                System.Collections.Generic.IList<RibbonPanel> panels = commandData.Application.GetRibbonPanels();
                int x = 0;
                RibbonPanel rp = null;
                while (x < panels.Count)
                {
                    rp = panels[x];
                    if (rp.Name == "ReVVed")
                        break;
                    x++;
                }
                if (rp == null)
                    throw new Exception("Options ribbon panel returned null");

                Options_Window ow = new Options_Window();
                IntPtr handle = commandData.Application.MainWindowHandle;
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(ow);
                wih.Owner = handle;
                ow.ShowDialog(); //most stop Revit or button visibility is set before settings are saved.
                log.InfoFormat("{0} opened Options dialog", Constants.GROUP_NAME);

                //set Ribbon Panel button visibility
                System.Collections.Generic.IList<RibbonItem> r_items = rp.GetItems();
                foreach (RibbonItem ri in r_items)
                {
                    switch (ri.Name)
                    {
                        case "dpHideButton":
                        case "dpShowButton":
                            ri.Visible = Properties.Settings.Default.ProjectCommander;
                            break;
                        case "pcButton":
                            ri.Visible = Properties.Settings.Default.ProjectCommander;
                            break;
                        case "mtButton":
                            ri.Visible = Properties.Settings.Default.MergeText;
                            break;
                        case "plButton":
                            ri.Visible = Properties.Settings.Default.PolyLine;
                            break;
                        case "wlButton":
                            ri.Visible = Properties.Settings.Default.WebLink;
                            break;
                        case "ccButton":
                            ri.Visible = Properties.Settings.Default.ChangeCase;
                            break;
                        case "ucButton":
                            ri.Visible = Properties.Settings.Default.UpperCase;
                            break;
                        case "ofButton":
                            ri.Visible = Properties.Settings.Default.OpenFolder;
                            break;
                        case "gfButton":
                            ri.Visible = Properties.Settings.Default.GridFlip;
                            break;
                        case "revButton":
                            ri.Visible = Properties.Settings.Default.Revisionist;
                            break;
                        case "coco2Button":
                            ri.Visible = Properties.Settings.Default.ComponentCommander;
                            break;
                        default:
                            break;
                    }
                }
                return Result.Succeeded;
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("OPT_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());

                return Result.Failed;
            }
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class revved_about : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(revved_about));

        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                //using new interface
                pkhCommon.Windows.About_Box ab = new pkhCommon.Windows.About_Box(this.GetType().Assembly);
                IntPtr handle = commandData.Application.MainWindowHandle;
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(ab);
                wih.Owner = handle;
                ab.ShowDialog();
                log.InfoFormat("{0} opened About dialog", Constants.GROUP_NAME);
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = "ReVVed" + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //{
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());
                //}
            }

            return Result.Succeeded;
        }
    }

    /// <summary>
    /// Returns true if Revit has a file open and it is NOT a family document
    /// </summary>
    public class projectopen_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            return !appdata.ActiveUIDocument.Document.IsFamilyDocument;
        }
    }

    //for help, about, options and project manager
    public class all_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            return true;
        }
    }
}