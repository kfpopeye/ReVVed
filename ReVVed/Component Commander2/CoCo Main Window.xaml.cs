using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RVVD.Component_Commander2
{
    internal enum Discipline { Arch, Struct, MEP };

    /// <summary>
    /// Interaction logic for CoCo_Main_Window.xaml
    /// </summary>
    public partial class CoCo_Main_Window : Window
    {
        private FamilyDataProvider FamilyDataProvider = null;
        private string CoCoPath = Constants.RevvedBasePath + "ComponentCommander\\";
        private FavoriteDataSet favorites = new FavoriteDataSet();

        internal Discipline ViewDiscipline
        {
            get { return (Discipline)GetValue(ViewDisciplineProperty); }
            set { SetValue(ViewDisciplineProperty, value); }
        }
        // Using a DependencyProperty as the backing store for ViewDiscipline.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewDisciplineProperty =
            DependencyProperty.Register("ViewDiscipline", typeof(Discipline), typeof(CoCo_Main_Window), new PropertyMetadata(Discipline.Arch));


        public CoCo_Main_Window(DataSetCreator dsc, FamilyDataProvider fdp, bool startDetailTab)
        {
            InitializeComponent();

            DataContext = dsc;
            FamilyDataProvider = fdp;
            FamilyPanel.DataContext = FamilyDataProvider;
            if (System.IO.File.Exists(CoCoPath + "favorites2.xml"))
                favorites.ReadXml(CoCoPath + "favorites2.xml");
            else
            {
                if (!Directory.Exists(CoCoPath))
                    Directory.CreateDirectory(CoCoPath);
                File.CreateText(CoCoPath + "favorites2.xml");
            }
            FavoritesTree.ItemsSource = favorites.Categories.DefaultView;
            if (startDetailTab)
                tabscontrol.SelectedIndex = 1; //set to detail component tab if opening from a drafting view
        }

        #region Functions
        private void SetButtons()
        {
            foreach (Button bt in ButtonPanel.Children)
            {
                if (bt == null || bt.Tag == null)
                    continue;

                if ((Discipline)bt.Tag == ViewDiscipline)
                    bt.IsEnabled = false;
                else
                    bt.IsEnabled = true;
            }
        }

        /// <summary>
        /// Checks category of model components. Detail components are filtered by datasetcreator
        /// </summary>
        private bool CategoryIsInActiveDiscipline(int category)
        {
            switch (ViewDiscipline)
            {
                case Discipline.Arch:
                    switch (category)
                    {
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_GenericModel:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_Casework:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_Columns:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_Entourage:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_Furniture:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_FurnitureSystems:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_LightingFixtures:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_MechanicalEquipment:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_Parking:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_Planting:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_PlumbingFixtures:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_Site:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_SpecialityEquipment:
                            return true;
                        default:
                            return false;
                    }
                case Discipline.Struct:
                    switch (category)
                    {
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_GenericModel:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_StructuralColumns:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_StructConnections:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFoundation:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_StructuralFraming:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_StructuralStiffener:
                            return true;
                        default:
                            return false;
                    }
                case Discipline.MEP:
                    switch (category)
                    {
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_GenericModel:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_DuctTerminal:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_CableTrayFitting:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_CommunicationDevices:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_ConduitFitting:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_DataDevices:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_DuctAccessory:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_DuctFitting:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_ElectricalEquipment:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_ElectricalFixtures:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_FireAlarmDevices:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_LightingDevices:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_LightingFixtures:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_NurseCallDevices:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_PipeAccessory:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_PipeFitting:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_SecurityDevices:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_Sprinklers:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_TelephoneDevices:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_MechanicalEquipment:
                        case (int)Autodesk.Revit.DB.BuiltInCategory.OST_PlumbingFixtures:
                            return true;
                        default:
                            return false;
                    }
                default:
                    return true;
            }
        }
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            favorites.WriteXml(CoCoPath + "favorites2.xml");
            Properties.Settings.Default.COCO2_Width = this.Width;
            Properties.Settings.Default.COCO2_Height = this.Height;
            Properties.Settings.Default.Save();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            BindingListCollectionView Tree_BLCV = null;
            Tree_BLCV = CollectionViewSource.GetDefaultView(ComponentTree.ItemsSource) as BindingListCollectionView;
            if (Tree_BLCV != null)
                Tree_BLCV.SortDescriptions.Add(
                    new System.ComponentModel.SortDescription("LocalizedName", System.ComponentModel.ListSortDirection.Ascending));
            FilterControl_Filter(null, null);
            SetButtons();

            this.Width = Properties.Settings.Default.COCO2_Width;
            this.Height = Properties.Settings.Default.COCO2_Height;
        }

        private void FilterControl_Filter(object sender, pkhCommon.WPF.FilterControlComponent.FilterEventArgs e)
        {
            string filterText = filterControl.FilterText;

            foreach (System.Data.DataRowView drv in ComponentTree.Items)
            {
                bool famVis = false;
                bool catVis = false;

                if (!string.IsNullOrEmpty(filterText))
                {
                    foreach (System.Data.DataRow F_dr in drv.Row.GetChildRows("Cat2Fam"))
                    {
                        bool symVis = false;
                        famVis = F_dr["FamilyName"].ToString().ToLower().Contains(filterText.ToLower()); //if family name matches filter

                        if (!famVis) // then all symbols should remain visible. So this block can be skipped
                        {
                            foreach (System.Data.DataRow S_dr in F_dr.GetChildRows("Fam2Sym"))
                            {
                                if (!S_dr["SymbolName"].ToString().ToLower().Contains(filterText.ToLower()))
                                {
                                    S_dr["Show"] = false;
                                    S_dr.AcceptChanges();
                                }
                                else
                                {
                                    symVis = true;
                                    catVis = true;
                                }
                            }

                            //if a symbol name matched the filter then the family must remain visible
                            if (!symVis)
                            {
                                F_dr["Show"] = false;
                                F_dr.AcceptChanges();
                            }
                        }
                        else
                            catVis = true;
                    }
                }
                else
                    catVis = true;

                if (!CategoryIsInActiveDiscipline((int)drv.Row["ID"]))
                {
                    drv.Row["Show"] = false;
                    drv.Row.AcceptChanges();
                }
                else if (!catVis)
                {
                    drv.Row["Show"] = false;
                    drv.Row.AcceptChanges();
                }
            }
        }

        /// <summary>
        /// resets the categories "show" to the current discipline setting and
        /// resets all family and symbol "show" to true.
        /// </summary>
        private void FilterControl_ClearFilter(object sender, RoutedEventArgs e)
        {
            foreach (System.Data.DataRowView drv in ComponentTree.Items)
            {
                drv.Row["Show"] = CategoryIsInActiveDiscipline((int)drv.Row["ID"]);
                drv.Row.AcceptChanges();
                foreach (DataRow F_dr in drv.Row.GetChildRows("Cat2Fam"))
                {
                    foreach (System.Data.DataRow S_dr in F_dr.GetChildRows("Fam2Sym"))
                    {
                        S_dr["Show"] = true;
                        S_dr.AcceptChanges();
                    }

                    F_dr["Show"] = true;
                    F_dr.AcceptChanges();
                }
            }
        }

        //sets the discipline in the component tree
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;
            ViewDiscipline = (Discipline)b.Tag;
            FilterControl_ClearFilter(null, null);
            FilterControl_Filter(null, null);
            SetButtons();
        }

        private void Add2FavButton_Click(object sender, RoutedEventArgs e)
        {
            FavoriteDataSet.CategoriesRow parentRow = null;

            favorites.BeginInit();
            //check if Category exists
            DataRow[] dr = favorites.Categories.Select(string.Format("Category = '{0}'", FamilyDataProvider.Category));
            if (dr.Length == 0)
            {
                parentRow = favorites.Categories.NewRow() as FavoriteDataSet.CategoriesRow;
                parentRow.Category = FamilyDataProvider.Category;
                favorites.Categories.AddCategoriesRow(parentRow);
                favorites.Categories.AcceptChanges();
            }
            else
                parentRow = dr[0] as FavoriteDataSet.CategoriesRow;

            FavoriteDataSet.FavoritesRow fr = favorites.Favorites.NewRow() as FavoriteDataSet.FavoritesRow;
            fr.Name = FamilyDataProvider.Name;
            fr.ParentID = parentRow.ID;
            fr.FilePath = FamilyDataProvider.FilePath;
            favorites.Favorites.AddFavoritesRow(fr);
            favorites.Favorites.AcceptChanges();
            favorites.EndInit();
        }

        /// <summary>
        /// Delete a symbol from the favorites list and deletes category if empty
        /// </summary>
        private void DeleteFav_Click(object sender, RoutedEventArgs e)
        {
            Button bt = sender as Button;
            DataRowView drv = bt.DataContext as DataRowView;
            DataRow dr = drv.Row.GetParentRow("Categories_Favorites");
            drv.Delete();
            dr.AcceptChanges();
            if (dr.GetChildRows("Categories_Favorites").Count() == 0)
                dr.Delete();
            favorites.AcceptChanges();
        }

        private void FolderButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process myProcess = new System.Diagnostics.Process();
            myProcess.StartInfo.FileName = System.IO.Path.GetDirectoryName(FamilyDataProvider.FilePath);
            myProcess.StartInfo.UseShellExecute = true;
            myProcess.StartInfo.RedirectStandardOutput = false;
            myProcess.Start();
            myProcess.Dispose();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            FamilyDataProvider.GetMode = FamilyDataProvider.Mode.Edit;
            this.Close();
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            FamilyDataProvider.GetMode = FamilyDataProvider.Mode.Reload;
            this.Close();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            FamilyDataProvider.GetMode = FamilyDataProvider.Mode.Select;
            this.Close();
        }

        /// <summary>
        /// Insert the double clicked symbol into the drawing.
        /// </summary>
        private void CompTreeViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            TreeViewItem tvi = sender as TreeViewItem;
            DataRowView drv = tvi.DataContext as DataRowView;
            if (drv.Row.Table.Columns.Contains("SymbolName")) //make sure a symbol was clicked
            {
                FamilyDataProvider.GetMode = FamilyDataProvider.Mode.Select;
                DialogResult = true;
                this.Close();
            }
        }

        private void CompTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tv = sender as TreeView;
            DataRowView drv = tv.SelectedItem as DataRowView;
            FamilyDataProvider.SetFamily((int)drv.Row["ID"], (bool)PreviewCB.IsChecked);
        }

        private void DetailTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tv = sender as TreeView;
            DataRowView drv = tv.SelectedItem as DataRowView;
            FamilyDataProvider.SetFamily((int)drv.Row["ID"]);
        }

        private void FavoritesTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeView tv = sender as TreeView;
            DataRowView drv = tv.SelectedItem as DataRowView;
            if (drv.Row.Table.Columns.Contains("FilePath")) //make sure a symbol was clicked
            {
                FileInfo rfaFile = new FileInfo((string)drv.Row["Filepath"]);

                if (rfaFile.Exists)
                {
                    FamilyDataProvider.SetFavorite(rfaFile, (bool)PreviewCB.IsChecked);
                }
                else
                {
                    //CoCo_DIA008
                    Autodesk.Revit.UI.TaskDialog td = new Autodesk.Revit.UI.TaskDialog(LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA008_Title")); //File missing
                    td.MainInstruction = LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA008_MainInstr") + rfaFile.Name; //Could not find the family file for the following favorite: 
                    td.MainContent = LocalizationProvider.GetLocalizedValue<string>("CoCo_DIA008_MainCont"); //Would you like to remove this entry from your saved favourites?
                    td.ExpandedContent = rfaFile.FullName;
                    td.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink1, LocalizationProvider.GetLocalizedValue<string>("TXT_Yes"));
                    td.AddCommandLink(Autodesk.Revit.UI.TaskDialogCommandLinkId.CommandLink2, LocalizationProvider.GetLocalizedValue<string>("TXT_No"));
                    Autodesk.Revit.UI.TaskDialogResult tdr = td.Show();

                    if (tdr == Autodesk.Revit.UI.TaskDialogResult.CommandLink1)
                    {
                        DataRow dr = drv.Row.GetParentRow("Categories_Favorites");
                        drv.Delete();
                        dr.AcceptChanges();
                        if (dr.GetChildRows("Categories_Favorites").Count() == 0)
                            dr.Delete();
                        favorites.AcceptChanges();
                    }
                }
            }
        }

        private void FavoritesTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {            
            DataRowView drv = FavoritesTree.SelectedItem as DataRowView;
            if (drv != null)
            {
                if (drv.Row.Table.Columns.Contains("FilePath")) //make sure a symbol was clicked
                {
                    FamilyDataProvider.GetMode = FamilyDataProvider.Mode.SelectFavorite;
                    DialogResult = true;
                    this.Close();
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void Help_Button_Click(object sender, RoutedEventArgs e)
        {
            Chelp.coco_help.Launch();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            Autodesk.Revit.UI.FileOpenDialog fod = new Autodesk.Revit.UI.FileOpenDialog(LocalizationProvider.GetLocalizedValue<string>("CoCo_FOD_Filter"));
            fod.ShowPreview = true;
            Autodesk.Revit.UI.ItemSelectionDialogResult res = fod.Show();
            if(res == Autodesk.Revit.UI.ItemSelectionDialogResult.Confirmed)
            {
                string s = Autodesk.Revit.DB.ModelPathUtils.ConvertModelPathToUserVisiblePath(fod.GetSelectedModelPath());
                FamilyDataProvider.SetFavorite(new FileInfo(s) , false);
                FamilyDataProvider.GetMode = FamilyDataProvider.Mode.LoadNew;
                DialogResult = true;
                this.Close();
            }
        }
    }

    public class ListSorterConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Collections.IList collection = value as System.Collections.IList;
            ListCollectionView view = new ListCollectionView(collection);
            System.ComponentModel.SortDescription sort = new System.ComponentModel.SortDescription(parameter.ToString(), System.ComponentModel.ListSortDirection.Ascending);
            view.SortDescriptions.Add(sort);

            return view;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}