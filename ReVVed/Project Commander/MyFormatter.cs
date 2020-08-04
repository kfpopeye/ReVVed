using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Documents;

namespace Project_Commander
{
  /// <summary>
  /// Removes rtf formatting for display only
  /// </summary>
    public class StripRtfFormatter : Xceed.Wpf.Toolkit.ITextFormatter
    {
        public string GetText(System.Windows.Documents.FlowDocument document)
        {
            return new TextRange(document.ContentStart, document.ContentEnd).Text;
        }

        public void SetText(System.Windows.Documents.FlowDocument document, string text)
        {
            System.Windows.Forms.RichTextBox rtb = new System.Windows.Forms.RichTextBox();
            rtb.Rtf = text;
            new TextRange(document.ContentStart, document.ContentEnd).Text = rtb.Text;
        }
    }
}
