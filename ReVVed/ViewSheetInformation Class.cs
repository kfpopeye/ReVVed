using System;
using Autodesk.Revit.DB;

namespace RVVD
{
    public class ViewSheetInformation
    {
        public bool selectedSheet { get; set; } //used to determine if revision should be added to sheet.
        public string sheetNumber { get; set; }
        public string sheetName { get; set; }
        public ElementId sheetId { get; set; }

        public ViewSheetInformation(ViewSheet vs)
        {
            this.selectedSheet = false;
            this.sheetNumber = vs.SheetNumber;
            this.sheetName = vs.Name;
            this.sheetId = vs.Id;
        }
    }
}
