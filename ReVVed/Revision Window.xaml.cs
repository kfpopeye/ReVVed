
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Autodesk.Revit.DB;
using System.Collections.ObjectModel;

namespace RVVD
{
	/// <summary>
	/// Interaction logic for Revision Window.xaml
	/// </summary>
	public partial class Revision_Window : Window
	{
		public ElementId selectedId = null;
		public ObservableCollection<ViewSheetInformation> theCollection = null;
		
		public Revision_Window(Revision[] revisions, ObservableCollection<ViewSheetInformation> coll)
		{
			InitializeComponent();
			
			bool first = true;
			foreach(Revision r in revisions)
			{
				if(r != null)
				{
					ComboBoxItem cbi = new ComboBoxItem();
					cbi.Tag = r.Id;
					cbi.Content = r.SequenceNumber.ToString() + " - " + r.Description;
					revCB.Items.Add(cbi);
					if(first)
					{
						revCB.SelectedItem = cbi;
						first = false;
					}
				}
			}

			theCollection = coll;
			sheetLB.BeginInit();
			sheetLB.ItemsSource = null;
			sheetLB.Items.Clear();
			sheetLB.ItemsSource = theCollection;	
			sheetLB.EndInit();
		}
		
		void comboBox1_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ComboBoxItem cbi = revCB.SelectedItem as ComboBoxItem;
			selectedId = cbi.Tag as ElementId;
			System.Diagnostics.Debug.WriteLine("Selected revision: " + cbi.Content.ToString());
		}
		
		void cancel_button_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = false;
			this.Close();
		}
		
		void OK_button_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
			this.Close();
		}
		
		void button1_Click(object sender, RoutedEventArgs e)
		{
			sheetLB.BeginInit();
			foreach(ViewSheetInformation vsi in theCollection)
			{
				vsi.selectedSheet = false;
			}
			sheetLB.EndInit();				
		}
		
		void select_Click(object sender, RoutedEventArgs e)
		{
			sheetLB.BeginInit();
			foreach(ViewSheetInformation vsi in theCollection)
			{
				vsi.selectedSheet = true;
			}
			sheetLB.EndInit();
		}

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            Title += " - DEBUG MODE";
#endif
        }
	}
}