using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

//Dataset Source: http://www.nullskull.com/q/10049447/wpf-and-dataset-xsdxscxss.aspx

namespace RVVD.Component_Commander2
{
    public class DataSetCreator
    {
        private static DataSet TheDataSet = null;
        private DataTable RvtCatTable = null;
        private DataTable RvtFamTable = null;
        private DataTable RvtSymTable = null;

        public DataSetCreator(ExternalCommandData comdata)
        {
            TheDataSet = new DataSet();
            RvtCatTable = new DataTable("RvtCat");
            RvtCatTable.Columns.Add("ID", typeof(int));
            RvtCatTable.Columns.Add("LocalizedName");
            RvtCatTable.Columns.Add("Show", typeof(bool));
            RvtCatTable.PrimaryKey = new DataColumn[] { RvtCatTable.Columns["ID"] };

            RvtFamTable = new DataTable("RvtFamilies");
            RvtFamTable.Columns.Add("ID", typeof(int));
            RvtFamTable.Columns.Add("CatId", typeof(int));
            RvtFamTable.Columns.Add("FamilyName");
            RvtFamTable.Columns.Add("Show", typeof(bool));
            RvtFamTable.PrimaryKey = new DataColumn[] { RvtFamTable.Columns["ID"] };

            RvtSymTable = new DataTable("RvtSymbols");
            RvtSymTable.Columns.Add("ID", typeof(int));
            RvtSymTable.Columns.Add("FamId", typeof(int));
            RvtSymTable.Columns.Add("SymbolName");
            RvtSymTable.Columns.Add("Show", typeof(bool));

            Document document = comdata.Application.ActiveUIDocument.Document;
            FilteredElementCollector collector = new FilteredElementCollector(document);
            collector.OfClass(typeof(Family));
            FilteredElementIterator itor = collector.GetElementIterator();

            itor.Reset();
            while (itor.MoveNext())
            {
                Family rfa = itor.Current as Family;
                if (rfa != null && !rfa.IsInPlace)
                {
                    addFamily(rfa);
                }
            }

            TheDataSet.Tables.Add(RvtCatTable);
            TheDataSet.Tables.Add(RvtFamTable);
            TheDataSet.Tables.Add(RvtSymTable);

            TheDataSet.Relations.Add(
                "Cat2Fam",
                TheDataSet.Tables["RvtCat"].Columns["ID"],
                TheDataSet.Tables["RvtFamilies"].Columns["CatId"]);
            TheDataSet.Relations.Add(
                "Fam2Sym",
                TheDataSet.Tables["RvtFamilies"].Columns["ID"],
                TheDataSet.Tables["RvtSymbols"].Columns["FamId"]);
        }

        private void addFamily(Family rfa)
        {
            DataRow dr = null;

            //add the category categories if not already created
            if (!RvtCatTable.Rows.Contains(rfa.FamilyCategory.Id.IntegerValue))
            {
                dr = RvtCatTable.NewRow();
                dr["ID"] = rfa.FamilyCategory.Id.IntegerValue;
                dr["LocalizedName"] = rfa.FamilyCategory.Name;
                dr["Show"] = true;
                RvtCatTable.Rows.Add(dr);
                System.Diagnostics.Debug.WriteLine(
                    string.Format("Added category {0} : {1}", rfa.FamilyCategory.Name, rfa.FamilyCategory.Id.IntegerValue));
            }

            //add the family
            dr = RvtFamTable.NewRow();
            dr["ID"] = rfa.Id.IntegerValue;
            dr["CatId"] = rfa.FamilyCategory.Id.IntegerValue;
            dr["FamilyName"] = rfa.Name;
            dr["Show"] = true;
            RvtFamTable.Rows.Add(dr);
            System.Diagnostics.Debug.WriteLine(
                string.Format("Added family {0} : {1}", rfa.Name, rfa.Id.IntegerValue));

            //loop through all the families symbols
            IEnumerator<ElementId> family_symbols_itor = rfa.GetFamilySymbolIds().GetEnumerator();
            if (family_symbols_itor != null)
            {
                while (family_symbols_itor.MoveNext())
                {
                    FamilySymbol fs = rfa.Document.GetElement(family_symbols_itor.Current) as FamilySymbol;
                    dr = RvtSymTable.NewRow();
                    dr["ID"] = fs.Id.IntegerValue;
                    dr["FamId"] = rfa.Id.IntegerValue;
                    dr["SymbolName"] = fs.Name;
                    dr["Show"] = true;
                    RvtSymTable.Rows.Add(dr);
                    System.Diagnostics.Debug.WriteLine(
                        string.Format("Added symbol {0} : {1}", fs.Name, fs.Id.IntegerValue));
                }
            }
        }

        public static DataSet GetDataSet()
        {
            return TheDataSet;
        }

        public static DataView GetDetailItems()
        {
            DataView dv = new DataView();
            dv.Table = TheDataSet.Tables["RvtCat"];
            dv.RowFilter = "ID = " + ((int)BuiltInCategory.OST_DetailComponents).ToString();
            return dv;
        }
    }
}
