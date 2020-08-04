/* Developed by pkh Lineworks */
using System;
using System.Windows;
using System.Text;
using System.Windows.Documents;
using RDB = Autodesk.Revit.DB;

namespace RVVD.Merge_Text
{
	/// <summary>
	/// Convert FormattedText class to\from FlowDocument class for editing in RTF Editors.
	/// </summary>
	public partial class FlowDocument_Converter
	{
		/// <summary>
		/// The amount to indent so characters align
		/// </summary>
		const double INDENT = 90d;    //depends an tab width in revit
		bool isBold = false, isUnderline = false, isItalic = false, isSuperscript = false, isSubscript = false;
		bool isList = false, isText = false;
		StringBuilder textRun = null;
		RDB.TextRange txtRange = null;
		RDB.FormattedText frmtTxt = null;
		FlowDocument flow_doc = null;
		Paragraph currentParagraph = null;
		List currentList = null;
		int currentIndent = -1;

        public FlowDocument_Converter() {}

		/// <summary>
		/// Converts textnote to flowdocument
		/// </summary>
		/// <param name="_value">The textnote to convert</param>
		/// <returns>flowdocument for use in RTF editor</returns>
		public FlowDocument Convert(RDB.TextNote _value)
		{
            resetGlobals();

			System.Diagnostics.Debug.WriteLine("Convert --------------------------------------");
			frmtTxt = _value.GetFormattedText();
			if (frmtTxt == null)
				return null;

            System.Diagnostics.Debug.WriteLine("Convert length: " + frmtTxt.GetPlainText().Length.ToString());
			
			flow_doc.FontSize = 16;
            flow_doc.Foreground = System.Windows.Media.Brushes.Black;
            RDB.Parameter fontp = _value.TextNoteType.get_Parameter(RDB.BuiltInParameter.TEXT_FONT);
            flow_doc.FontFamily = new System.Windows.Media.FontFamily(fontp.AsString());
            RDB.Parameter param = _value.TextNoteType.get_Parameter(RDB.BuiltInParameter.TEXT_FONT);
			param = _value.get_Parameter(RDB.BuiltInParameter.TEXT_ALIGN_HORZ);
			switch(param.AsInteger())
			{
				case (int)RDB.TextAlignFlags.TEF_ALIGN_LEFT:
					flow_doc.TextAlignment = TextAlignment.Left;
					break;
				case (int)RDB.TextAlignFlags.TEF_ALIGN_CENTER:
					flow_doc.TextAlignment = TextAlignment.Center;
					break;
				case (int)RDB.TextAlignFlags.TEF_ALIGN_RIGHT:
					flow_doc.TextAlignment = TextAlignment.Right;
					break;
				default:
					break;
			}
            Style style = new Style(typeof(Paragraph));
            style.Setters.Add(new Setter(Block.MarginProperty, new Thickness(0)));
            flow_doc.Resources.Add(typeof(Paragraph), style);
            style = new Style(typeof(List));
            style.Setters.Add(new Setter(List.MarginProperty, new Thickness(0)));
            style.Setters.Add(new Setter(List.PaddingProperty, new Thickness(20, double.NaN, double.NaN, double.NaN)));
            flow_doc.Resources.Add(typeof(List), style);

            ListItem lastListItem = null;

			while(txtRange.Start != (frmtTxt.AsTextRange().End - 1))
			{
				char currentChar = frmtTxt.GetPlainText(txtRange)[0];
				
				if(frmtTxt.GetListType(txtRange) != RDB.ListType.None) //------------process lists
				{
					int indent = frmtTxt.GetIndentLevel(txtRange);
					if(currentIndent == -1)
						currentIndent = indent;
					
					if(isText)
					{
						completeCurrentRun();
						if(currentParagraph.Inlines.Count != 0)
						{
							if(currentParagraph.Inlines.LastInline.GetType() == typeof(LineBreak))
								currentParagraph.Inlines.Remove(currentParagraph.Inlines.LastInline);
							flow_doc.Blocks.Add(currentParagraph);
							currentParagraph = new Paragraph();
							System.Diagnostics.Debug.WriteLine("Added paragraph to flowdocument.");
						}
						isText = false;
					}
					
					if(!isList)
					{
						currentList = new List();
                        currentList.Padding = new Thickness(20, double.NaN, double.NaN, double.NaN);
                        //TODO: right aligned lists do not display the same as Revit. Padding?
                        System.Diagnostics.Debug.WriteLine("Created new list.");
						switch (frmtTxt.GetListType(txtRange))
						{
							case RDB.ListType.ArabicNumbers:
								currentList.MarkerStyle = TextMarkerStyle.Decimal;
								currentList.StartIndex = frmtTxt.GetListStartNumber(txtRange);
								break;
							case RDB.ListType.Bullet:
								currentList.MarkerStyle = TextMarkerStyle.Disc;
								break;
							case RDB.ListType.LowerCaseLetters:
								currentList.MarkerStyle = TextMarkerStyle.LowerLatin;
								currentList.StartIndex = frmtTxt.GetListStartNumber(txtRange);
								break;
							case RDB.ListType.UpperCaseLetters:
								currentList.MarkerStyle = TextMarkerStyle.UpperLatin;
								currentList.StartIndex = frmtTxt.GetListStartNumber(txtRange);
								break;
							default:
                                throw new NotSupportedException("Unknown list type: " + frmtTxt.GetListType(txtRange).ToString());
						}
					}
					
					isList = true;
					checkFormatting();
					
					if(indent > currentIndent)
					{
						lastListItem.Blocks.Add(createSubList());
						currentChar = frmtTxt.GetPlainText(txtRange)[0];
					}
					
					if (currentChar.Equals('\r') || currentChar.Equals('\n'))
					{
						//	add listitem to currentList
						completeCurrentRun();
						lastListItem = new ListItem(currentParagraph);
						currentList.ListItems.Add(lastListItem);
						System.Diagnostics.Debug.WriteLine("Added list item.");
						currentParagraph = new Paragraph();
					}
				}
				else	//------------process text
				{
					if(isList)
					{
						//	add listitem to currentList and current list to flowdocument
						flow_doc.Blocks.Add(currentList);
						System.Diagnostics.Debug.WriteLine("Added list to flowdocument.");
						isList = false;
					}
					
					isText = true;
					checkFormatting();
				}
				
				if (!currentChar.Equals('\r') && !currentChar.Equals('\n'))
					textRun.Append(currentChar);
				else if(isText)
				{
					completeCurrentRun();
                    flow_doc.Blocks.Add(currentParagraph);
                    System.Diagnostics.Debug.WriteLine("Added paragraph to flowdocument.");
                    System.Diagnostics.Debug.WriteLine("\tIndent: " + currentParagraph.TextIndent);
                    currentParagraph = new Paragraph();
				}
				
				txtRange.Start++;
			}   //end while
			
			if(isList)
			{
				//	add listitem to currentList and current list to flowdocument
				completeCurrentRun();
				currentList.ListItems.Add(new ListItem(currentParagraph));
				flow_doc.Blocks.Add(currentList);
				System.Diagnostics.Debug.WriteLine("Added final list to flowdocument.");
			}
			
			if(isText)
			{
				completeCurrentRun();
				if(currentParagraph.Inlines.Count != 0)
				{
					if(currentParagraph.Inlines.LastInline.GetType() == typeof(LineBreak))
						currentParagraph.Inlines.Remove(currentParagraph.Inlines.LastInline);
                    flow_doc.Blocks.Add(currentParagraph);
					System.Diagnostics.Debug.WriteLine("Added final paragraph to flowdocument.");
				}
			}

#if DEBUG
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            using (System.IO.FileStream fs = new System.IO.FileStream(desktop + @"\Temp\Merged-" +_value.Id.ToString() + ".xml", System.IO.FileMode.Create))
            {
                TextRange range = new TextRange(flow_doc.ContentStart, flow_doc.ContentEnd);
                range.Save(fs, DataFormats.Xaml);
            }
#endif

            return flow_doc;
		}
		
		/// <summary>
		/// updates formatting booleans and completes run when formatting changes
		/// </summary>
		private void checkFormatting()
        {
            //Check Bold Status if formatting has changed -> start new run
            if (frmtTxt.GetBoldStatus(txtRange) != RDB.FormatStatus.None && !isBold) //bold begins
			{
				completeCurrentRun();
				isBold = true;
			}
			else if(frmtTxt.GetBoldStatus(txtRange) == RDB.FormatStatus.None && isBold) //bold ends
			{
				completeCurrentRun();
				isBold = false;
			}
			
			//Check underline Status if formatting has changed -> start new run
			if(frmtTxt.GetUnderlineStatus(txtRange) != RDB.FormatStatus.None && !isUnderline) //underline begins
			{
				completeCurrentRun();
				isUnderline = true;
			}
			else if(frmtTxt.GetUnderlineStatus(txtRange) == RDB.FormatStatus.None && isUnderline) //underline ends
			{
				completeCurrentRun();
				isUnderline = false;
			}
			
			//Check Italic Status if formatting has changed -> start new run
			if(frmtTxt.GetItalicStatus(txtRange) != RDB.FormatStatus.None && !isItalic) //Italic begins
			{
				completeCurrentRun();
				isItalic = true;
			}
			else if(frmtTxt.GetItalicStatus(txtRange) == RDB.FormatStatus.None && isItalic) //Italic ends
			{
				completeCurrentRun();
				isItalic = false;
			}
			
			//Check Superscript Status if formatting has changed -> start new run
			if(frmtTxt.GetSuperscriptStatus(txtRange) != RDB.FormatStatus.None && !isSuperscript) //Superscript begins
			{
				completeCurrentRun();
				isSuperscript = true;
			}
			else if(frmtTxt.GetSuperscriptStatus(txtRange) == RDB.FormatStatus.None && isSuperscript) //Superscript ends
			{
				completeCurrentRun();
				isSuperscript = false;
			}
			
			//Check Subscript Status if formatting has changed -> start new run
			if(frmtTxt.GetSubscriptStatus(txtRange) != RDB.FormatStatus.None && !isSubscript) //Subscript begins
			{
				completeCurrentRun();
				isSubscript = true;
			}
			else if(frmtTxt.GetSubscriptStatus(txtRange) == RDB.FormatStatus.None && isSubscript) //Subscript ends
			{
				completeCurrentRun();
				isSubscript = false;
			}
		}
		
		/// <summary>
		/// A recursive function to iterate through the chars and create any sublists that occur.
		/// These are detected by the indent level.
		/// </summary>
		/// <returns>A sublist or list with sublists</returns>
		private List createSubList()
		{
			int checkIndent = frmtTxt.GetIndentLevel(txtRange);
			List thisList = new List();
            //set indent here
            System.Diagnostics.Debug.WriteLine("Created new sublist.");
			switch (frmtTxt.GetListType(txtRange))
			{
				case RDB.ListType.ArabicNumbers:
					thisList.MarkerStyle = TextMarkerStyle.Decimal;
					thisList.StartIndex = frmtTxt.GetListStartNumber(txtRange);
					break;
				case RDB.ListType.Bullet:
					thisList.MarkerStyle = TextMarkerStyle.Disc;
					break;
				case RDB.ListType.LowerCaseLetters:
					thisList.MarkerStyle = TextMarkerStyle.LowerLatin;
					thisList.StartIndex = frmtTxt.GetListStartNumber(txtRange);
					break;
				case RDB.ListType.UpperCaseLetters:
					thisList.MarkerStyle = TextMarkerStyle.UpperLatin;
					thisList.StartIndex = frmtTxt.GetListStartNumber(txtRange);
					break;
				default:
					break;
			}
			
			ListItem lastListItem = null;
			while(txtRange.Start != frmtTxt.AsTextRange().End
			      &&
			      frmtTxt.GetListType(txtRange) != RDB.ListType.None)
			{
				char c = frmtTxt.GetPlainText(txtRange)[0];
				int indent = frmtTxt.GetIndentLevel(txtRange);
				
				if(indent > checkIndent)
				{
					lastListItem.Blocks.Add(createSubList());
					thisList.ListItems.Add(lastListItem);
					c = frmtTxt.GetPlainText(txtRange)[0];
				}
				else if(indent < checkIndent)
					return thisList;//return list
				
				if (c.Equals('\r') || c.Equals('\n'))
				{
					//	add listitem to this list
					completeCurrentRun();
					lastListItem = new ListItem(currentParagraph);
					thisList.ListItems.Add(lastListItem);
					System.Diagnostics.Debug.WriteLine("Added list item to sublist.");
					currentParagraph = new Paragraph();
				}
				else
					textRun.Append(c);
				
				txtRange.Start++;
			}
			
			return thisList;
		}
		
		/// <summary>
		/// Turns the Stringbuilder into a Run, surrounds it with formatting classes, adds it to currentParagraph and clears the Stringbuilder
		/// </summary>
		void completeCurrentRun()
		{
			if(textRun.Length == 0)
				return;
			
			Inline i = new Run(textRun.ToString());
			System.Diagnostics.Debug.WriteLine("Created run: " + textRun.ToString());
			if(isSuperscript)
			{
				i.BaselineAlignment = BaselineAlignment.Superscript;
				i.FontSize = flow_doc.FontSize / 2;
			}
			if(isSubscript)
			{
				i.BaselineAlignment = BaselineAlignment.Subscript;
				i.FontSize = flow_doc.FontSize / 2;
			}

			if(isBold)
				i.FontWeight = FontWeights.Bold;
			if(isUnderline)
				i.TextDecorations = TextDecorations.Underline;
			if(isItalic)
				i.FontStyle = FontStyles.Italic;

            currentParagraph.Inlines.Add(i);

            if (!isList)
            {
                currentParagraph.TextIndent = frmtTxt.GetIndentLevel(txtRange) * INDENT;
                System.Diagnostics.Debug.WriteLine("Paragraph indent = " + currentParagraph.TextIndent);
            }

            textRun.Clear();
		}

        void resetGlobals()
        {
            isBold = false; isUnderline = false; isItalic = false; isSuperscript = false; isSubscript = false;
            isList = false; isText = false;
            textRun = new StringBuilder();
            txtRange = new RDB.TextRange(0, 1);
            frmtTxt = null;
            flow_doc = new FlowDocument();
            currentParagraph = new Paragraph();
            currentList = null;
            currentIndent = -1;
        }
	}
}