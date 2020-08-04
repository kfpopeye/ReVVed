using System.Windows;
using System.Windows.Documents;
using System.Collections.Generic;
using System;
using System.IO;

namespace RVVD.Merge_Text
{
    public partial class Merge_Text_Window : Window
    {
        private void update_preview()
        {
            try
            {
                if (_notesList == null)
                    return;
                
                int lastElement = _notesList.Count;
                int thisElement = 0;
                bool previousBlockisParagraph = false;

                mergedDocument = new FlowDocument();
                mergedDocument.FontFamily = new System.Windows.Media.FontFamily(theFont);
                mergedDocument.FontSize = 24d;
                Style style = new Style(typeof(Paragraph));
                style.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0)));
                mergedDocument.Resources.Add(typeof(Paragraph), style);
                style = new Style(typeof(List));
                style.Setters.Add(new Setter(List.MarginProperty, new Thickness(0)));
                style.Setters.Add(new Setter(List.PaddingProperty, new Thickness(35, double.NaN, double.NaN, double.NaN)));
                mergedDocument.Resources.Add(typeof(List), style);

                foreach (Element el in _notesList)
                {
                    thisElement++;
                    FlowDocument tempDoc = new FlowDocument();
                    pkhCommon.WPF.FlowDocumentHelpers.AddDocument(el._theDoc, tempDoc);
                    int lastBlock = tempDoc.Blocks.Count;
                    int thisBlock = 0;

                    foreach (Block b in tempDoc.Blocks)
                    {
                        thisBlock++;
                        if (b is List)
                        {
                            if (thisElement > 1 && !previousBlockisParagraph && !((bool)MT_cb_AddReturns.IsChecked)) //add a space to split adjacent lists
                                mergedDocument.Blocks.Add(new Paragraph(new Run(" ")));
                            pkhCommon.WPF.FlowDocumentHelpers.AddBlock(b, mergedDocument);
                            previousBlockisParagraph = false;
                        }
                        else
                        {
                            bool endsWithPeriod = false;
                            Paragraph p = b as Paragraph;

                            if (!previousBlockisParagraph || (bool)MT_cb_PreserveReturns.IsChecked)
                            {
                                mergedDocument.Blocks.Add(new Paragraph());
                                if ((bool)MT_cb_PreserveReturns.IsChecked)
                                    (mergedDocument.Blocks.LastBlock as Paragraph).TextIndent = p.TextIndent;
                                previousBlockisParagraph = true;
                            }

                            foreach (Inline inl in p.Inlines)
                            {
                                if (!(inl is LineBreak))
                                {
                                    pkhCommon.WPF.FlowDocumentHelpers.AddInline(inl, mergedDocument);
                                    TextRange tr = new TextRange(inl.ContentStart, inl.ContentEnd);
                                    endsWithPeriod = tr.Text.EndsWith(".");
                                }
                            }

                            if (thisElement != lastElement || thisBlock != lastBlock)
                                if ((bool)MT_cb_AddPeriods.IsChecked && !endsWithPeriod)
                                    pkhCommon.WPF.FlowDocumentHelpers.AddInline(new Run(". "), mergedDocument);  //add a period and space where paragraphs used to end
                                else
                                    pkhCommon.WPF.FlowDocumentHelpers.AddInline(new Run(" "), mergedDocument);  //add a space where paragraphs used to end
                            else if (thisElement == lastElement && thisBlock == lastBlock && !endsWithPeriod)
                                if ((bool)MT_cb_AddPeriods.IsChecked)
                                    pkhCommon.WPF.FlowDocumentHelpers.AddInline(new Run("."), mergedDocument);  //add a period only where last paragraph used to end
                        }
                    }

                    if ((bool)MT_cb_AddReturns.IsChecked)
                    {
                        if (mergedDocument.Blocks.LastBlock is Paragraph)
                        {
                            Paragraph p = mergedDocument.Blocks.LastBlock as Paragraph;
                            p.Inlines.Add(new LineBreak());
                        }
                        else    //if list
                        {
                            Paragraph p = new Paragraph(new Run(" "));
                            mergedDocument.Blocks.Add(p);
                        }
                    }
                }

                clearFormatting(mergedDocument.Blocks);
                TextRange tr2 = new TextRange(mergedDocument.ContentStart, mergedDocument.ContentEnd);
                tr2.ApplyPropertyValue(Span.FontFamilyProperty, theFont);
                ChangeCase();
                preview_window.Document = mergedDocument;
            }
            catch (Exception err)
            {
                if (mergedDocument != null && mergedDocument.Blocks.Count != 0)
                {
                    string file = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "\\ReVVed\\MergeText_error.txt");
                    Directory.CreateDirectory(Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "\\ReVVed\\"));
                    using (FileStream fs = new FileStream(file, System.IO.FileMode.Create))
                    {
                        TextRange range = new TextRange(mergedDocument.ContentStart, mergedDocument.ContentEnd);
                        range.Save(fs, DataFormats.Xaml);
                    }
                }
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(err).Throw();
                throw;
            }
        }

        private void clearFormatting(BlockCollection bc)
        {
            foreach (Block b in bc)
            {
                b.ClearValue(Block.FontFamilyProperty);
                b.ClearValue(Block.FontSizeProperty);
                b.ClearValue(Block.PaddingProperty);
                if (b is List)
                    recurseLists(b as List);
                else if (b is Paragraph)
                    recurseParagraph(b as Paragraph);
                else if (b is Section)
                    clearFormatting((b as Section).Blocks);
            }
        }

        private void recurseParagraph(Paragraph _para)
        {
            _para.ClearValue(Paragraph.FontFamilyProperty);
            _para.ClearValue(Paragraph.FontSizeProperty);
            _para.ClearValue(Paragraph.PaddingProperty);
            foreach (Inline i in _para.Inlines)
            {
                if (i.BaselineAlignment != BaselineAlignment.Baseline) //is super\subscript
                {
                    i.ClearValue(Inline.FontFamilyProperty);
                    i.FontSize = 12d;
                }
                else
                {
                    i.ClearValue(Inline.FontFamilyProperty);
                    i.ClearValue(Inline.FontSizeProperty);
                }
            }
        }

        private void recurseLists(List _list)
        {
            _list.ClearValue(List.FontFamilyProperty);
            _list.ClearValue(List.FontSizeProperty);
            _list.ClearValue(List.PaddingProperty);
            foreach (ListItem li in _list.ListItems)
            {
                foreach (Block b in li.Blocks)
                {
                    if (b is List)
                        recurseLists(b as List);
                    else if (b is Paragraph)
                        recurseParagraph(b as Paragraph);
                }
            }
        }

        private void ChangeCase()
        {
            if (_caseChanger == null)
                return;

            List<Inline> inlines = GetInlines(mergedDocument.Blocks);
            for (int x = 0; x < inlines.Count; x++)
            {
                Inline i = inlines[x];
                if (i is Run)
                {
                    Run r = i as Run;
                    r.Text = _caseChanger(r.Text);
                }
            }
        }

        private List<Inline> GetInlines(BlockCollection _blocks)
        {
            var inlines = new List<Inline>();

            foreach (var block in _blocks)
            {
                
                if (block is Paragraph)
                {
                    var paragraph = block as Paragraph;
                    inlines.AddRange(paragraph.Inlines);
                }
                if (block is List)
                {
                    var list = block as List;
                    inlines.AddRange(getInlinesFromList(list));
                }
                if(block is Section)
                {
                    var section = block as Section;
                    inlines.AddRange(GetInlines(section.Blocks));
                }
            }
            return inlines;
        }

        private List<Inline> getInlinesFromList(List _list)
        {
            var inlines = new List<Inline>();
            foreach (ListItem li in _list.ListItems)
            {
                foreach (Block b in li.Blocks)
                {
                    if (b is List)
                        inlines.AddRange(getInlinesFromList(b as List));
                    else if (b is Paragraph)
                        inlines.AddRange((b as Paragraph).Inlines);
                }
            }
            return inlines;
        }
    }
}