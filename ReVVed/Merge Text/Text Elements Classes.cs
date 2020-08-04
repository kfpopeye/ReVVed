using System.Collections.ObjectModel;
using System.Windows.Documents;
using Autodesk.Revit.DB;

namespace RVVD.Merge_Text
{
    public class Text_Elements
    {
        public ObservableCollection<Element> _theList = null;
        public int maximumIndentLevel { get; set; }
        public int maximumStartLevel { get; set; }
        public int minimumStartLevel { get; set; }
        public string theFont = string.Empty;

        internal Text_Elements(Autodesk.Revit.DB.ElementSet noteset)
        {
            _theList = new ObservableCollection<Element>();
            FlowDocument_Converter fdConv = new FlowDocument_Converter();

            // add selected notes to the merge text form
            foreach (Autodesk.Revit.DB.Element theItem in noteset)
            {
                if (theItem.Category != null)
                {
                    int ID = theItem.Id.IntegerValue;
                    Autodesk.Revit.DB.TextNote note = theItem.Document.GetElement(theItem.Id) as TextNote;

                    maximumIndentLevel = note.GetFormattedText().GetMaximumIndentLevel();   //these 3 lines are used in "Merge Text Functions.cs"
                    maximumStartLevel = note.GetFormattedText().GetMaximumListStartNumber();
                    minimumStartLevel = note.GetFormattedText().GetMinimumListStartNumber();

                    Parameter fontp = note.TextNoteType.get_Parameter(BuiltInParameter.TEXT_FONT);
                    string font = fontp.AsString();
                    if(theFont == string.Empty)
                        theFont = font;
                    FlowDocument fd = fdConv.Convert(note);
                    _theList.Add(new Element() { _theID = ID, _theFont = font, _theDoc = fd });
                }
            }
        }
    }

    public class Element
    {
        public FlowDocument _theDoc { get; set; }
        public string _theText
        {
            get
            {
                System.Windows.Documents.TextRange tr = new System.Windows.Documents.TextRange(_theDoc.ContentStart, _theDoc.ContentEnd);
                return tr.Text;
            }
            set {; }
        }
        public int _theID { get; set; }
        public string _theFont { get; set; }

        internal Element()
        {
        }
    }
}
