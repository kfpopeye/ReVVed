using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using Debug = System.Diagnostics.Debug;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RVVD
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class poly_line : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(poly_line));

        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            /*
             * 
             * User can select multiple JOINED lines, this routine will will determine the total length of the lines
             * and open a window to prompt user which end of the line to extend\trim and what the new total length should be.
             * 
             * IDEAS
             * ------
             * 1) Add support for arcs, ellipses and splines.
             * 
             */

            Debug.WriteLine(Constants.POLY_LINE_MACRO_NAME + " command begins.");
            Autodesk.Revit.DB.Document actdoc = commandData.Application.ActiveUIDocument.Document;
            Autodesk.Revit.DB.View activeview = actdoc.ActiveView;
            ElementSet sel = new ElementSet();

            foreach (ElementId eid in commandData.Application.ActiveUIDocument.Selection.GetElementIds())
            {
                sel.Insert(actdoc.GetElement(eid));
            }

            Transaction transaction = new Transaction(actdoc);
            transaction.Start(LocalizationProvider.GetLocalizedValue<string>("P_Title"));

            try
            {
                //using (Measurement_Protocol mp = new Measurement_Protocol())
                //{
                //    mp.sendAnalytic(Constants.POLY_LINE_MACRO_NAME, commandData);
                //}
                foreach (Element e in sel)
                {
                    LocationCurve lc = e.Location as LocationCurve;
                    Line l = lc.Curve as Line;

                    if (e.Category.Id.IntegerValue != (int)BuiltInCategory.OST_Lines || l == null) // Filter the non-line type and non-line elements in the selection set
                        sel.Erase(e);
                }

                if (sel.IsEmpty || sel.Size < 2)
                {
                    TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("P_Title"), 
                        LocalizationProvider.GetLocalizedValue<string>("P_DIA001_MainInstr")); //Less than 2 lines were selected.
                    transaction.RollBack();
                    return Result.Failed;
                }
                else
                {
                    Polyline.Line_Manager lnManager = new Polyline.Line_Manager(sel, activeview);
                    if (lnManager.HasPath)
                    {
                        Polyline.Polyline_Window LMform = new Polyline.Polyline_Window(lnManager, actdoc);
                        IntPtr handle = commandData.Application.MainWindowHandle;
                        System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(LMform);
                        wih.Owner = handle;
                        LMform.ShowDialog();

                        if ((bool)LMform.DialogResult)
                        {
                            // create new line at the proper length
                            bool result = lnManager.SetPolylineLength(LMform.segmentEnd, LMform.polylineLength, actdoc);
                            if (result == false)
                            {
                                TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("P_Title"));
                                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("P_DIA002_MainInstr"); //The line could not be created.
                                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("P_DIA002_MainCont"); //Something went wrong when Revit attempted to change the line to your specified dimension.
                                td.Show();
                                transaction.RollBack();
                                log.InfoFormat("{0} command failed.", Constants.POLY_LINE_MACRO_NAME);
                                Debug.WriteLine(Constants.POLY_LINE_MACRO_NAME + " command failed.");
                                return Result.Failed;
                            }
                            else
                            {
                                transaction.Commit();
                                Debug.WriteLine(Constants.POLY_LINE_MACRO_NAME + " command succeeded.");
                                return Result.Succeeded;
                            }
                        }
                        else
                        {
                            transaction.RollBack();
                            Debug.WriteLine(Constants.POLY_LINE_MACRO_NAME + " command cancelled.");
                            return Result.Cancelled;
                        }
                    }
                    else
                    {
                        log.InfoFormat("{0} could not find a valid path", Constants.POLY_LINE_MACRO_NAME);
                        TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("P_Title"));
                        td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("P_DIA003_MainInstr"); //A path could not be found.
                        td.MainContent = LocalizationProvider.GetLocalizedValue<string>("P_DIA003_MainCont"); //Please make sure you select more than one line, the lines are connect and do not form a closed loop. Arcs, ellipses and splines are currently not supported.
                        td.Show();
                        transaction.RollBack();
                        Debug.WriteLine(Constants.POLY_LINE_MACRO_NAME + " command failed.");
                        return Result.Failed;
                    }
                }
            }
            catch (Exception err)
            {
                log.Error("Unexpected exception", err);

                Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("P_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                td.ExpandedContent = err.ToString();
                //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                TaskDialogResult tdr = td.Show();

                //if (tdr == TaskDialogResult.CommandLink1)
                //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, err, this.GetType().Assembly.GetName());

                transaction.RollBack();
                Debug.WriteLine(Constants.POLY_LINE_MACRO_NAME + " command threw exception.");
                return Result.Failed;
            }
        }
    }

    public class pl_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            if (appdata.ActiveUIDocument.Document.IsFamilyDocument)
                return false;

            if (appdata.ActiveUIDocument.Selection.GetElementIds().Count < 2)
                return false;
            if (selectCatagories.Size > 1)
                return false;
            foreach (Category c in selectCatagories)
            {
                if (c.Id.IntegerValue == (int)BuiltInCategory.OST_Lines)
                    return true;
            }

            return false;
        }
    }
}