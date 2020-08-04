/* Developed by pkh Lineworks */
using System;
using System.Windows.Documents;

namespace RVVD.Merge_Text
{
	/// <summary>
	/// Description of FlowDocument2String_Converter.
	/// </summary>
	public class FlowDocument2String_Converter
	{
		System.Text.StringBuilder theStrgBldr = new System.Text.StringBuilder();
		
		public FlowDocument2String_Converter()
		{
		}
		
		public string Convert2String (FlowDocument _flowDoc)
		{
			foreach(Block theBlock in _flowDoc.Blocks)
			{
				if(theBlock is Paragraph)
				{
					ConvertParagraph(theBlock as Paragraph);
				}
				else if(theBlock is List)
				{
					ConvertList(theBlock as List);
                }
                else if (theBlock is Section)
                {
                    Section s = theBlock as Section;
                    ConvertList(s.Blocks.FirstBlock as List);
                }
                else
					throw new NotSupportedException("FlowDocument2String_Converter Found unhandled block type in FlowDocument: " + theBlock.GetType().ToString());
			}

			return theStrgBldr.ToString();
		}

		private void ConvertList(List _theList)
		{
			foreach(ListItem li in _theList.ListItems)
			{
				foreach(Block theBlock in li.Blocks)
				{
					if(theBlock.GetType() == typeof(Paragraph))
					{
						ConvertParagraph(theBlock as Paragraph);
					}
					else if(theBlock.GetType() == typeof(List))
					{
						ConvertList(theBlock as List);
					}
					else
						throw new NotSupportedException("FlowDocument2String_Converter found unhandled block type in FlowDocument: " + theBlock.GetType().ToString());
				}
			}
		}

		private void ConvertParagraph(Paragraph _theParagraph)
		{
			foreach(Inline inl in _theParagraph.Inlines)
			{
				checkFormatting(inl);
			}
			theStrgBldr.Append('\r');
        }

		private void checkFormatting(Inline inl)
		{
			if(inl.GetType() == typeof(Bold))
			{
				Span s = inl as Span;
				checkFormatting(s.Inlines.FirstInline);
			}
			if(inl.GetType() == typeof(Underline))
			{
				Span s = inl as Span;
				checkFormatting(s.Inlines.FirstInline);
			}
			if(inl.GetType() == typeof(Italic))
			{
				Span s = inl as Span;
				checkFormatting(s.Inlines.FirstInline);
			}
			if(inl.GetType() == typeof(Run))
			{
				Run r = inl as Run;
				convertRun(r);
			}
		}

		private void convertRun(Run _run)
		{
			foreach(char c in _run.Text)
			{
				theStrgBldr.Append(c);
			}
		}
	}
}
