/* Developed by pkh Lineworks */
using System;
using System.Windows;
using System.Windows.Documents;
using Autodesk.Revit.DB;

namespace RVVD.Merge_Text
{
    public partial class FlowDocument_Converter
    {
        private ListType currentListType = ListType.None;
        private int listStartNumber = -1;
        private bool isFirstList = true;

        public FormattedText ConvertBack(FlowDocument _flowDoc)
        {
            try
            {
                resetGlobals();
                System.Diagnostics.Debug.WriteLine("ConvertBack --------------------------------------");
                flow_doc = _flowDoc;
                frmtTxt = new FormattedText();
                txtRange = new Autodesk.Revit.DB.TextRange(0, 1);

                FlowDocument2String_Converter f2sConv = new FlowDocument2String_Converter();
                string s = f2sConv.Convert2String(flow_doc);

                frmtTxt.SetPlainText(s);
                System.Diagnostics.Debug.WriteLine("Plain text length: " + s.Length.ToString());
                System.Diagnostics.Debug.WriteLine("------------------");
                System.Diagnostics.Debug.WriteLine(s);
                System.Diagnostics.Debug.WriteLine("------------------");

                foreach (Block theBlock in flow_doc.Blocks)
                {
                    if (theBlock is Paragraph)
                    {
                        isText = true;
                        currentIndent = -1;
                        ConvertParagraph(theBlock as Paragraph);
                    }
                    else if (theBlock is List)
                    {
                        isList = true;
                        currentIndent = -1;
                        ConvertList(theBlock as List, isFirstList);
                        isFirstList = false;
                        currentListType = ListType.None;
                        listStartNumber = -1;
                    }
                    else if (theBlock is Section)
                    {
                        Section sct = theBlock as Section;
                        foreach (Block sb in sct.Blocks)
                        {
                            if (sb is List)
                            {
                                isList = true;
                                currentIndent = -1;
                                List l = sb as List;
                                ConvertList(l, isFirstList);
                                isFirstList = false;
                                currentListType = ListType.None;
                                listStartNumber = -1;
                            }
                            else if (sb is Paragraph)
                            {
                                isText = true;
                                currentIndent = -1;
                                ConvertParagraph(sb as Paragraph);
                            }
                        }
                    }
                    else
                        throw new NotSupportedException("Found unhandled block type in FlowDocument: " + theBlock.GetType().ToString());

                    isText = false;
                    isList = false;
                }

                System.Diagnostics.Debug.WriteLine("Textrange start= " + txtRange.Start.ToString());

                return frmtTxt;
            }
            catch (Exception)
            {
                if (_flowDoc != null && _flowDoc.Blocks.Count != 0)
                {
                    string file = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\ReVVed\\MergeText_error.txt";
                    using (System.IO.FileStream fs = new System.IO.FileStream(file, System.IO.FileMode.Create))
                    {
                        System.Windows.Documents.TextRange range = new System.Windows.Documents.TextRange(_flowDoc.ContentStart, _flowDoc.ContentEnd);
                        range.Save(fs, DataFormats.Xaml);
                    }
                }
                throw;
            }
        }

        private void ConvertList(List _theList, bool _topLevelList)
        {
            if (_topLevelList)
                listStartNumber = _theList.StartIndex;
            ListType old_list_type = ListType.None;

            switch (_theList.MarkerStyle)
            {
                case TextMarkerStyle.Decimal:
                    currentListType = ListType.ArabicNumbers;
                    break;
                case TextMarkerStyle.Disc:
                    currentListType = ListType.Bullet;
                    break;
                case TextMarkerStyle.LowerLatin:
                    currentListType = ListType.LowerCaseLetters;
                    break;
                case TextMarkerStyle.UpperLatin:
                    currentListType = ListType.UpperCaseLetters;
                    break;
                default:
                    break;
            }

            foreach (ListItem li in _theList.ListItems)
            {
                foreach (Block theBlock in li.Blocks)
                {
                    if (theBlock is Paragraph)
                    {
                        ConvertParagraph(theBlock as Paragraph);
                    }
                    else if (theBlock is List)
                    {
                        int old_lsn = listStartNumber;
                        old_list_type = currentListType;
                        listStartNumber = -1;
                        int old_indent = currentIndent;
                        currentIndent++;
                        ConvertList(theBlock as List, false);
                        currentIndent = old_indent;
                        currentListType = old_list_type;
                        if (_topLevelList)
                            listStartNumber = old_lsn;
                    }
                    else
                        throw new NotSupportedException("ConvertList found unhandled block type in FlowDocument: " + theBlock.GetType().ToString());
                }
            }
        }

        private void ConvertParagraph(Paragraph _theParagraph)
        {
            if (currentIndent == -1)
                currentIndent = (int)(_theParagraph.TextIndent / INDENT);
            foreach (Inline inl in _theParagraph.Inlines)
            {
                checkFormatting(inl);
            }
            txtRange.Start++;   //for newlines at end of paragraph
        }

        private void checkFormatting(Inline inl)
        {
            System.Diagnostics.Debug.Write("checkFormatting found a: ");
            System.Diagnostics.Debug.WriteLine(inl.GetType().ToString());

            if (inl is Bold)
            {
                isBold = true;
                Span s = inl as Span;
                checkFormatting(s.Inlines.FirstInline);
            }
            if (inl is Underline)
            {
                isUnderline = true;
                Span s = inl as Span;
                checkFormatting(s.Inlines.FirstInline);
            }
            if (inl is Italic)
            {
                isItalic = true;
                Span s = inl as Span;
                checkFormatting(s.Inlines.FirstInline);
            }
            if (inl is Run)
            {
                Run r = inl as Run;
                if (r.FontWeight == FontWeights.Bold)
                    isBold = true;
                if (r.TextDecorations != DependencyProperty.UnsetValue && r.TextDecorations.Count > 0)
                    isUnderline = true;
                if (r.FontStyle == FontStyles.Italic)
                    isItalic = true;
                if (r.BaselineAlignment == BaselineAlignment.Superscript)
                    isSuperscript = true;
                if (r.BaselineAlignment == BaselineAlignment.Subscript)
                    isSubscript = true;
                convertRun(r);
            }
        }

        private void convertRun(Run _run)
        {
            System.Diagnostics.Debug.WriteLine("Indent: " + currentIndent.ToString());
            System.Diagnostics.Debug.WriteLine("txtRange position: " + txtRange.Start);
            System.Diagnostics.Debug.Write("convertRun: ");
            foreach (char c in _run.Text)
            {
                if (Char.IsLetterOrDigit(c))
                    System.Diagnostics.Debug.Write(c);
                else
                    System.Diagnostics.Debug.Write(" " + Char.GetUnicodeCategory(c).ToString() + " ");
                setFormats();
                //TODO:what if user inputs a tab into RTB
                txtRange.Start++;
            }
            System.Diagnostics.Debug.Write(Environment.NewLine);
            System.Diagnostics.Debug.WriteLine("txtRange position: " + txtRange.Start);

            isBold = false; isUnderline = false; isItalic = false; isSuperscript = false; isSubscript = false;
        }

        private void setFormats()
        {
            frmtTxt.SetListType(txtRange, currentListType); //set this first or indents wont work
            frmtTxt.SetIndentLevel(txtRange, currentIndent);
            frmtTxt.SetBoldStatus(txtRange, isBold);
            frmtTxt.SetUnderlineStatus(txtRange, isUnderline);
            frmtTxt.SetItalicStatus(txtRange, isItalic);
            frmtTxt.SetSubscriptStatus(txtRange, isSubscript);
            frmtTxt.SetSuperscriptStatus(txtRange, isSuperscript);
            if (listStartNumber != -1 && currentListType != ListType.Bullet)
                frmtTxt.SetListStartNumber(txtRange, listStartNumber);
        }
    }
}