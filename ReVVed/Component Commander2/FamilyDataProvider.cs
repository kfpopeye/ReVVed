using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Data;
using System.IO;

namespace RVVD.Component_Commander2
{
    public class FamilyDataProvider : INotifyPropertyChanged
    {
        internal enum Mode { Edit, Reload, Select, SelectFavorite, LoadNew };
        private Document ActiveDoc = null;
        public BitmapSource PreviewImage { get; set; }
        public System.Collections.ObjectModel.ObservableCollection<ParameterInfo> ParameterCollection { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        internal Mode GetMode { get; set; }

        int symbolId = -1;
        public int CurrentSymbolId
        {
            get
            {
                return symbolId;
            }
            set
            {
                symbolId = value;
                OnPropertyChanged(new PropertyChangedEventArgs("CurrentSymbolId"));
            }
        }

        public string FilePath = null;
        public bool HasFilePath
        {
            get
            {
                return !string.IsNullOrEmpty(FilePath);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        public FamilyDataProvider(Document d)
        {
            ActiveDoc = d;
            ParameterCollection = new System.Collections.ObjectModel.ObservableCollection<ParameterInfo>();
            CurrentSymbolId = -1;
        }

        /// <summary>
        /// Gets information about the currently selected component to display to user
        /// </summary>
        /// <param name="eid">ElementId of currently selected component</param>
        /// <param name="makePreview">Should a preview image be created</param>
        public void SetFamily(int eid, bool makePreview)
        {
            FileInfo rfaFile = null;
            System.Drawing.Bitmap bitmap = null;
            ElementType et = ActiveDoc.GetElement(new ElementId(eid)) as ElementType;
            ParameterCollection.Clear();

            if (et != null)
            {
                if (et is FamilySymbol)
                {
                    FamilySymbol fs = et as FamilySymbol;
                    Document famdoc = ActiveDoc.EditFamily(fs.Family);
                    FilePath = famdoc.PathName;
                    famdoc.Close(false);
                    Category = fs.Family.FamilyCategory.Name;
                    Name = fs.FamilyName;
                    CurrentSymbolId = eid;
                }
                else
                {
                    FilePath = null;
                    CurrentSymbolId = -1;
                }

                if (HasFilePath)
                {
                    rfaFile = new FileInfo(FilePath);
                    if (rfaFile != null && rfaFile.Exists)
                    {
                        ParameterInfo pi = new ParameterInfo()
                        {
                            ParameterGroup = "File Information",
                            ParameterName = "Path",
                            ParameterValue = rfaFile.DirectoryName
                        };
                        ParameterCollection.Add(pi);
                        pi = new ParameterInfo()
                        {
                            ParameterGroup = "File Information",
                            ParameterName = "Size",
                            ParameterValue = rfaFile.Length.ToString()
                        };
                        ParameterCollection.Add(pi);
                        pi = new ParameterInfo()
                        {
                            ParameterGroup = "File Information",
                            ParameterName = "Last Modified",
                            ParameterValue = rfaFile.LastWriteTime.ToString()
                        };
                        ParameterCollection.Add(pi);
                    }
                    else
                    {
                        ParameterInfo pi = new ParameterInfo()
                        {
                            ParameterGroup = "File Information",
                            ParameterName = "Path",
                            ParameterValue = "No file path"
                        };
                        ParameterCollection.Add(pi);
                    }
                }

                if (makePreview)
                {
                    if (rfaFile != null && rfaFile.Exists)
                    {
                        StructuredStorage.Storage stor = new StructuredStorage.Storage(rfaFile.FullName);
                        bitmap = new System.Drawing.Bitmap(stor.ThumbnailImage.GetPreviewAsImage());
                    }
                    else
                        bitmap = et.GetPreviewImage(new System.Drawing.Size(128, 128));
                }

                if (bitmap == null)
                    PreviewImage = null;
                else
                    PreviewImage = pkhCommon.WPF.Converters.BitmapToBitmapSource.ToBitmapSource(bitmap);


                foreach (Parameter p in et.GetOrderedParameters())
                {
                    ParameterInfo pi = new ParameterInfo()
                    {
                        ParameterGroup = LabelUtils.GetLabelFor(p.Definition.ParameterGroup),
                        ParameterName = p.Definition.Name,
                        ParameterValue = GetParameterInformation(p)
                    };
                    ParameterCollection.Add(pi);
                }
            }
            else
            {
                PreviewImage = null;
                FilePath = null;
                CurrentSymbolId = -1;
            }
            OnPropertyChanged(new PropertyChangedEventArgs("HasFilePath"));
            OnPropertyChanged(new PropertyChangedEventArgs("ParameterCollection"));
            OnPropertyChanged(new PropertyChangedEventArgs("PreviewImage"));
        }

        /// <summary>
        /// Gets information about the currently selected DETAIL component to display to user
        /// </summary>
        /// <param name="eid">ElementId of currently selected component</param>
        public void SetFamily(int eid)
        {
            FileInfo rfaFile = null;
            ElementType et = ActiveDoc.GetElement(new ElementId(eid)) as ElementType;
            ParameterCollection.Clear();

            if (et != null)
            {
                if (et is FamilySymbol)
                {
                    FamilySymbol fs = et as FamilySymbol;
                    Document famdoc = ActiveDoc.EditFamily(fs.Family);
                    FilePath = famdoc.PathName;
                    famdoc.Close(false);
                    Category = fs.Family.FamilyCategory.Name;
                    Name = fs.FamilyName;
                    CurrentSymbolId = eid;
                }
                else
                {
                    FilePath = null;
                    CurrentSymbolId = -1;
                }

                if (HasFilePath)
                {
                    rfaFile = new FileInfo(FilePath);
                    if (rfaFile != null && rfaFile.Exists)
                    {
                        ParameterInfo pi = new ParameterInfo()
                        {
                            ParameterGroup = "File Information",
                            ParameterName = "Path",
                            ParameterValue = rfaFile.DirectoryName
                        };
                        ParameterCollection.Add(pi);
                        pi = new ParameterInfo()
                        {
                            ParameterGroup = "File Information",
                            ParameterName = "Size",
                            ParameterValue = rfaFile.Length.ToString()
                        };
                        ParameterCollection.Add(pi);
                        pi = new ParameterInfo()
                        {
                            ParameterGroup = "File Information",
                            ParameterName = "Last Modified",
                            ParameterValue = rfaFile.LastWriteTime.ToString()
                        };
                        ParameterCollection.Add(pi);
                    }
                    else
                    {
                        ParameterInfo pi = new ParameterInfo()
                        {
                            ParameterGroup = "File Information",
                            ParameterName = "Path",
                            ParameterValue = "No file path"
                        };
                        ParameterCollection.Add(pi);
                    }
                }

                foreach (Parameter p in et.GetOrderedParameters())
                {
                    ParameterInfo pi = new ParameterInfo()
                    {
                        ParameterGroup = LabelUtils.GetLabelFor(p.Definition.ParameterGroup),
                        ParameterName = p.Definition.Name,
                        ParameterValue = GetParameterInformation(p)
                    };
                    ParameterCollection.Add(pi);
                }

                PreviewImage = null;
            }
            OnPropertyChanged(new PropertyChangedEventArgs("HasFilePath"));
            OnPropertyChanged(new PropertyChangedEventArgs("ParameterCollection"));
            OnPropertyChanged(new PropertyChangedEventArgs("PreviewImage"));
        }

        internal void SetFavorite(FileInfo rfaFile, bool makePreview)
        {
            ParameterCollection.Clear();

            FilePath = rfaFile.FullName;
            rfaFile = new FileInfo(FilePath);
            if (rfaFile != null && rfaFile.Exists)
            {
                ParameterInfo pi = new ParameterInfo()
                {
                    ParameterGroup = "File Information",
                    ParameterName = "Path",
                    ParameterValue = rfaFile.DirectoryName
                };
                ParameterCollection.Add(pi);
                pi = new ParameterInfo()
                {
                    ParameterGroup = "File Information",
                    ParameterName = "Size",
                    ParameterValue = rfaFile.Length.ToString()
                };
                ParameterCollection.Add(pi);
                pi = new ParameterInfo()
                {
                    ParameterGroup = "File Information",
                    ParameterName = "Last Modified",
                    ParameterValue = rfaFile.LastWriteTime.ToString()
                };
                ParameterCollection.Add(pi);
            }
            else
            {
                ParameterInfo pi = new ParameterInfo()
                {
                    ParameterGroup = "File Information",
                    ParameterName = "Exists",
                    ParameterValue = "No"
                };
                ParameterCollection.Add(pi);
            }

            if (makePreview)
            {
                StructuredStorage.Storage stor = new StructuredStorage.Storage(rfaFile.FullName);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(stor.ThumbnailImage.GetPreviewAsImage());
                PreviewImage = pkhCommon.WPF.Converters.BitmapToBitmapSource.ToBitmapSource(bitmap);
            }

            OnPropertyChanged(new PropertyChangedEventArgs("HasFilePath"));
            OnPropertyChanged(new PropertyChangedEventArgs("ParameterCollection"));
            OnPropertyChanged(new PropertyChangedEventArgs("PreviewImage"));
        }

        private String GetParameterInformation(Parameter para)
        {
            string defName = null;
            // Use different method to get parameter data according to the storage type
            switch (para.StorageType)
            {
                case StorageType.Double:
                    defName = para.AsValueString();
                    break;
                case StorageType.ElementId:
                    defName = para.AsValueString();
                    break;
                case StorageType.Integer:
                    if (ParameterType.YesNo == para.Definition.ParameterType)
                    {
                        if (para.AsInteger() == 0)
                        {
                            defName = "False";
                        }
                        else
                        {
                            defName = "True";
                        }
                    }
                    else
                    {
                        defName = para.AsInteger().ToString();
                    }
                    break;
                case StorageType.String:
                    defName = para.AsString();
                    break;
                default:
                    defName = "Unexposed parameter.";
                    break;
            }
            return defName;
        }
    }

    public class ParameterInfo
    {
        public string ParameterGroup { get; set; }
        public string ParameterName { get; set; }
        public string ParameterValue { get; set; }

        public ParameterInfo() { }
    }
}
