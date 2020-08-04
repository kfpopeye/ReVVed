using System;
using System.IO;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RVVD
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class component_commander2 : IExternalCommand
    {
        private readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(component_commander2));

        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Autodesk.Revit.UI.Selection.Selection sel = commandData.Application.ActiveUIDocument.Selection;
            Autodesk.Revit.DB.Document actdoc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                //using (Measurement_Protocol mp = new Measurement_Protocol())
                //{
                //    mp.sendAnalytic(Constants.COMP_COMM_MACRO_NAME, commandData);
                //}
                /*
                 * get all components of a certain type
                 * call compnent manager function to add to list(try to keep manager generic)
                 * pass component manager to dialog
                 */

                Component_Commander2.FamilyDataProvider fdp = new Component_Commander2.FamilyDataProvider(actdoc);
                Component_Commander2.DataSetCreator C_Mngr = new Component_Commander2.DataSetCreator(commandData);

                Component_Commander2.CoCo_Main_Window mainWindow = new Component_Commander2.CoCo_Main_Window(C_Mngr, fdp, actdoc.ActiveView.ViewType == ViewType.DraftingView);
                IntPtr handle = commandData.Application.MainWindowHandle;
                System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(mainWindow);
                wih.Owner = handle;

                //set active discipline based on Revit flavour
                switch (actdoc.Application.Product)
                {
                    case Autodesk.Revit.ApplicationServices.ProductType.MEP:
                        mainWindow.ViewDiscipline = Component_Commander2.Discipline.MEP;
                        break;
                    case Autodesk.Revit.ApplicationServices.ProductType.Structure:
                        mainWindow.ViewDiscipline = Component_Commander2.Discipline.Struct;
                        break;
                    default:
                        mainWindow.ViewDiscipline = Component_Commander2.Discipline.Arch;
                        break;
                }

                if ((bool)mainWindow.ShowDialog())
                {
                    switch (fdp.GetMode)
                    {
                        case Component_Commander2.FamilyDataProvider.Mode.SelectFavorite: //load and place family
                            Family fam;
                            FamilySymbol fs;
                            Transaction t = new Transaction(actdoc, LocalizationProvider.GetLocalizedValue<string>("CoCo_EC_TransAct")); //Load family
                            t.Start();
                            if (!actdoc.LoadFamily(fdp.FilePath, new LoadingOptions(), out fam))
                            {
                                t.RollBack();
                                //CoCo_DIA001
                                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA001_Title")); //Load failed
                                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA001_MainInstr"); //Family was identical to saved file.
                                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA001_MainCont") + " " + fdp.FilePath; //No need to reload family from:
                                td.Show();
                            }
                            else
                            {
                                t.Commit();
                                _log.InfoFormat("Used {0} to load and place family symbol - {1}", Constants.COMP_COMM_MACRO_NAME, fdp.Name);
                                IEnumerator<ElementId> e = fam.GetFamilySymbolIds().GetEnumerator();
                                e.MoveNext();
                                fs = actdoc.GetElement(e.Current as ElementId) as FamilySymbol;
                                commandData.Application.ActiveUIDocument.PostRequestForElementTypePlacement(fs);
                            }
                            break;
                        case Component_Commander2.FamilyDataProvider.Mode.Select: //place family
                            FamilySymbol theSymbol = actdoc.GetElement(new ElementId(fdp.CurrentSymbolId)) as FamilySymbol;
                            _log.InfoFormat("Used {0} to place family symbol - {1}", Constants.COMP_COMM_MACRO_NAME, fdp.Name);
                            commandData.Application.ActiveUIDocument.PostRequestForElementTypePlacement(theSymbol);
                            break;
                        case Component_Commander2.FamilyDataProvider.Mode.Edit: //edit family
                            if (!System.IO.File.Exists(fdp.FilePath))
                            {
                                //CoCo_DIA002
                                TaskDialog.Show(
                                    LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA002_Title"), //Edit failed
                                    LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA002_MainInstr") + " " + fdp.FilePath); //Could not find the file at: 
                            }
                            else
                            {
                                _log.InfoFormat("Used {0} to edit family - {1}", Constants.COMP_COMM_MACRO_NAME, fdp.FilePath);
                                commandData.Application.OpenAndActivateDocument(fdp.FilePath);
                            }
                            break;
                        case Component_Commander2.FamilyDataProvider.Mode.Reload:   //reload family
                            if (!System.IO.File.Exists(fdp.FilePath))
                            {
                                //CoCo_DIA003
                                TaskDialog.Show(
                                    LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA003_Title"), //Reload failed
                                    LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA002_MainInstr") + fdp.FilePath); //Could not find the file at: 
                            }
                            else
                            {
                                Transaction tr = new Transaction(actdoc, LocalizationProvider.GetLocalizedValue<string>("CoCo_EC_TransAct")); //Load family
                                tr.Start();
                                if (actdoc.LoadFamily(fdp.FilePath, new LoadingOptions(), out Family fs1))
                                {
                                    _log.InfoFormat("Used {0} to reload family - {1}", Constants.COMP_COMM_MACRO_NAME, fdp.FilePath);
                                    //CoCo_DIA004
                                    TaskDialog.Show(
                                        LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA004_Title"), //Reloaded
                                        LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA004_MainInstr") + " " + fs1.Name); //Successfully reloaded: 
                                    tr.Commit();
                                }
                                else
                                {
                                    tr.RollBack();
                                    //CoCo_DIA005
                                    TaskDialog tdr = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA003_Title")); //Reload failed
                                    tdr.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA005_MainInstr"); //Family was identical to saved file.
                                    tdr.MainContent = LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA005_MainCont") + " " + fdp.FilePath; //No need to reload family from: 
                                    tdr.Show();
                                }
                            }
                            break;
                        case Component_Commander2.FamilyDataProvider.Mode.LoadNew:   //load new? family
                            if (!System.IO.File.Exists(fdp.FilePath))
                            {
                                //CoCo_DIA006
                                TaskDialog.Show(
                                    LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA001_Title"), //Load failed
                                    LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA006_MainInstr") + " " + fdp.FilePath); //Could not find the file at: 
                            }
                            else
                            {
                                Transaction tr = new Transaction(actdoc, LocalizationProvider.GetLocalizedValue<string>("CoCo_EC_TransAct")); //Load family
                                tr.Start();
                                if (actdoc.LoadFamily(fdp.FilePath, new LoadingOptions(), out Family fam1))
                                {
                                    _log.InfoFormat("Used {0} to load family - {1}", Constants.COMP_COMM_MACRO_NAME, fdp.FilePath);
                                    IEnumerator<ElementId> e = fam1.GetFamilySymbolIds().GetEnumerator();
                                    e.MoveNext();
                                    fs = actdoc.GetElement(e.Current as ElementId) as FamilySymbol;
                                    tr.Commit();
                                    commandData.Application.ActiveUIDocument.PostRequestForElementTypePlacement(fs);
                                }
                                else
                                {
                                    tr.RollBack();
                                    //CoCo_DIA007
                                    TaskDialog tdr = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA001_Title")); //Load failed
                                    tdr.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA005_MainInstr"); //Family was identical to saved file.
                                    tdr.MainContent = LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA007_MainCont") + " " + fdp.FilePath; //No need to load family from: 
                                    tdr.Show();
                                }
                            }
                            break;
                    }
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.InvalidOperationException err)
            {
                _log.Warn("Error inserting family", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("CoCo_Error_Title")); //Place Family Error
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("CoCo_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst"); //The *APP NAME* has encountered an error and cannot complete.
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("CoCo_Error_MainCont"); //Some possible reasons are this document is not active, or this is a family document and the instances of this family symbol can not exist in the current family, or this family symbol has no command to create instance, or the command to create instance is disabled in active view.
                td.ExpandedContent = err.ToString();
                td.MainIcon = TaskDialogIcon.TaskDialogIconNone;
                td.Show();
                return Result.Cancelled;
            }
            catch (Exception err)
            {
                _log.Error(err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title")); //Unexpected Error
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("CoCo_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst"); //The *APP NAME* has encountered an error and cannot complete.
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont"); //Please use the link below to send a bug report to the developer. You will be able to see the bug report before you send it.
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1")); //Send bug report.
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());
                return Result.Cancelled;
            }
        }
    }

    /// <summary>
    /// Reloads a family and overrwrites parameter values.
    /// </summary>
    public class LoadingOptions : IFamilyLoadOptions
    {
        public LoadingOptions()
        {
        }

        public bool OnFamilyFound(bool familyInUse, out bool overwriteParameterValues)
        {
            overwriteParameterValues = true;
            return true;
        }

        public bool OnSharedFamilyFound(Family sharedFamily, bool familyInUse, out FamilySource source, out bool overwriteParameterValues)
        {
            overwriteParameterValues = false;
            source = FamilySource.Project;
            return true;
        }
    }

    public class coco2_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            switch (appdata.ActiveUIDocument.ActiveView.ViewType)
            {
                case ViewType.ColumnSchedule:
                case ViewType.CostReport:
                case ViewType.DrawingSheet:
                case ViewType.Legend:
                case ViewType.LoadsReport:
                case ViewType.PanelSchedule:
                case ViewType.PresureLossReport:
                case ViewType.ProjectBrowser:
                case ViewType.Rendering:
                case ViewType.Report:
                case ViewType.Schedule:
                case ViewType.SystemBrowser:
                case ViewType.Walkthrough:
                    return false;
                default:
                    return true;
            }
        }
    }
}