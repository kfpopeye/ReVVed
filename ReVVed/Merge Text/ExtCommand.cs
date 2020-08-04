using System;
using System.Collections.Generic;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RVVD
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class merge_text : IExternalCommand
    {
        private readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(merge_text));

        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {             
            Autodesk.Revit.DB.Document actdoc = commandData.Application.ActiveUIDocument.Document;
            ICollection<ElementId> selected = commandData.Application.ActiveUIDocument.Selection.GetElementIds();
            Transaction transaction = null;

            if (selected.Count == 0)
            {
                TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("MT_Title"), 
                    LocalizationProvider.GetLocalizedValue<string>("MT_DIA001")); //No items were selected.
                return Result.Cancelled;
            }
            else
            {
                try
                {
                    //using (Measurement_Protocol mp = new Measurement_Protocol())
                    //{
                    //    mp.sendAnalytic(Constants.MERGE_TEXT_MACRO_NAME, commandData);
                    //}
                    // Filter the non-text elements in the note set
                    ElementSet noteSet = new ElementSet();
                    foreach (ElementId eid in selected)
                    {
                        Element e = actdoc.GetElement(eid);
                        if (e.Category.Id.IntegerValue == (int)BuiltInCategory.OST_TextNotes)
                            noteSet.Insert(e);
                    }

                    //Check to see if items have been selected by user
                    if (noteSet.Size == 0)
                    {
                        TaskDialog.Show(LocalizationProvider.GetLocalizedValue<string>("MT_Title"), 
                            LocalizationProvider.GetLocalizedValue<string>("MT_DIA002")); //No notes were selected.
                        return Result.Failed;
                    }

                    Merge_Text.Text_Elements mtte = new Merge_Text.Text_Elements(noteSet);

                    Merge_Text.Merge_Text_Window form1 = new Merge_Text.Merge_Text_Window(mtte);
                    IntPtr handle = commandData.Application.MainWindowHandle;
                    System.Windows.Interop.WindowInteropHelper wih = new System.Windows.Interop.WindowInteropHelper(form1);
                    wih.Owner = handle;
                    form1.ShowDialog();
                    if ((bool)form1.DialogResult)
                    {
                        transaction = new Transaction(actdoc);
                        transaction.Start(LocalizationProvider.GetLocalizedValue<string>("MT_Title"));
                        int x = 0;
                        // delete old notes from the project
                        foreach (Merge_Text.Element theItem in form1._notesList)
                        {
                            if (x == 0)
                            {
                                //change first note to new merged note
                                TextNote note = actdoc.GetElement(new ElementId(theItem._theID)) as TextNote;
                                Merge_Text.FlowDocument_Converter conv = new Merge_Text.FlowDocument_Converter();
                                FormattedText ft = conv.ConvertBack(form1.get_MergedDocument());
                                note.SetFormattedText(ft);
                                x++;
                            }
                            else
                                actdoc.Delete(new ElementId(theItem._theID));
                        }
                        transaction.Commit();
                        log.InfoFormat("{0} merged {1} notes", Constants.MERGE_TEXT_MACRO_NAME, noteSet.Size.ToString());
                        return Result.Succeeded;
                    }
                }
                catch (Exception err)
                {
                    log.Error("Unexpected exception", err);

                    Autodesk.Revit.UI.TaskDialog td = new TaskDialog(LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Title"));
                    td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("MT_Title") + LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainInst");
                    td.MainContent = LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_MainCont");
                    td.ExpandedContent = err.ToString();
                    //td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("ErrorDialog_Command1"));
                    td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                    TaskDialogResult tdr = td.Show();

                    //if (tdr == TaskDialogResult.CommandLink1)
                    //{
                    //    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    //    sb.AppendLine(err.ToString());
                    //    sb.AppendLine();
                    //    string file = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ReVVed\\MergeText_error.txt";
                    //    if (System.IO.File.Exists(file))
                    //    {
                    //        sb.AppendLine(System.IO.File.ReadAllText(file));
                    //        System.IO.File.Delete(file);
                    //    }                        
                    //    pkhCommon.Email.SendErrorMessage(commandData.Application.Application.VersionName, commandData.Application.Application.VersionBuild, sb.ToString(), this.GetType().Assembly.GetName());
                    //}
                    if (transaction != null && transaction.HasStarted())
                        transaction.RollBack();
                    return Result.Failed;
                }
            }
            return Result.Succeeded;
        }
    }

    public class mt_AvailableCheck : IExternalCommandAvailability
    {
        public bool IsCommandAvailable(UIApplication appdata, CategorySet selectCatagories)
        {
            if (appdata.Application.Documents.Size == 0)
                return false;

            if (appdata.ActiveUIDocument.Selection.GetElementIds().Count < 2)
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
}