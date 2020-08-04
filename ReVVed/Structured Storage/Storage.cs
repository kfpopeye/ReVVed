
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;

namespace RVVD.StructuredStorage
{

 public class Storage : StorageBase, IDisposable
 {

  #region Private Variables

  private BasicFileInfo _basicInfo;
  private PreviewImage _thumbnailImage;

  #endregion

  #region Constructors

  public Storage(string fileName) 
   : base(fileName)
  {
   if(File.Exists(fileName) == false)
   {
    throw new FileNotFoundException(string.Format("The file \"{0}\" was not found.", fileName));
   }
   FileName = fileName;
   OpenStructuredStorageFile();
   if(IsInitialized == false)
   {
    if(StorageRoot != null)
    {
     CloseStorageRoot(StorageRoot);
    }
    return;
   }
   BasicInfo = new BasicFileInfo(FileName, StorageRoot);
   ThumbnailImage = new PreviewImage(FileName, StorageRoot);
   if(StorageRoot != null)
   {
    CloseStorageRoot(StorageRoot);
   }
  }

  #endregion

  #region IDisposable Implementation

  private bool _disposed;

  /// <summary>
  /// <para>This method handles disposal of the OleDbAccess object.</para>
  /// </summary>
  /// <param name="disposing">A flag to check for redundant calls.</param>
  protected virtual void Dispose(bool disposing)
  {
   if(_disposed == false)
   {
    // Release Unmanaged Resources
    if(disposing)
    {
     // Release Managed Resources
     if(StorageRoot != null)
     {
      CloseStorageRoot(StorageRoot);
     }
    }

    _disposed = true;
   }
  }

  /// <summary>
  /// <para></para>
  /// </summary>
  public void Dispose()
  {
   Dispose(true);
   GC.SuppressFinalize(this);
  }

  // This code is used to help detect any instances of this class that are not
  // properly disposed. If this class is properly disposed, the finalizer is
  // suppressed in the Dispose() method above and the finalizer method below
  // is never called by runtime garbabe collection. If an instance of this
  // is not properly disposed, the finalizer will be called, and the
  // Debug.Assert method will display the StackTrace info, the thread and the time.
#if DEBUG

  //  /// <summary>
  //  /// <para>Finalizer method for the OleDbAccess class.</para>
  //  /// </summary>
  //  /// <remarks><para>
  //  /// This should never be called if the object is properly disposed. 
  //  /// This method should never be removed or commented out.
  //  /// </para></remarks>
  //  ~UserPage()
  //  {
  ////   string newLine = Environment.NewLine;
  ////   string disposeFailMessage = string.Format("StackTrace for Disposal Failure: {0}{1}The Thread Name for the Disposal Failure: {2}{3}The Time of the Disposal Failure: {4}{5} @ {6}", 
  ////    newLine, _debugStackTrace.ToString(), 
  ////    newLine, _debugThreadName, 
  ////    newLine, _debugTime.ToShortDateString(),  _debugTime.ToShortTimeString());
  ////   LogManager.LogMessage(disposeFailMessage, LogManager.LogSeverityType.Error);
  ////   Debug.Assert(false, disposeFailMessage);
  //  }

#endif

  #endregion

  #region Public Properties

  public BasicFileInfo BasicInfo
  {
   get
   {
    if(_basicInfo.IsInitialized == false)
    {
     _basicInfo.ReadStructuredStorageFile();
    }
    return _basicInfo;
   }
   set
   {
    _basicInfo = value;
   }
  }

  public PreviewImage ThumbnailImage
  {
   get
   {
    if(_thumbnailImage.IsInitialized == false)
    {
     _thumbnailImage.ReadStructuredStorageFile();
    }
    return _thumbnailImage;
   }
   set
   {
    _thumbnailImage = value;
   }
  }

  //public bool IsInitialized
  //{
  // get
  // {
  //  return _isInitialized;
  // }
  // protected set
  // {
  //  _isInitialized = value;
  // }
  //}

  //public string FileName
  //{
  // get
  // {
  //  return _fileName;
  // }
  // protected set
  // {
  //  _fileName = value;
  // }
  //}

  //public string FileExtension
  //{
  // get
  // {
  //  if(string.IsNullOrEmpty(FileName))
  //  {
  //   return string.Empty;
  //  }

  //  return Path.GetExtension(FileName).Replace(".", string.Empty);
  // }
  //}

  //public DocumentType DocType
  //{
  // get
  // {
  //  if(string.IsNullOrEmpty(FileExtension))
  //  {
  //   return DocumentType.Unknown;
  //  }

  //  switch(FileExtension.ToUpper())
  //  {
  //   case "RVT":
  //    return DocumentType.Project;

  //   case "RTE":
  //    return DocumentType.ProjectTemplate;

  //   case "RFA":
  //    return DocumentType.Family;

  //   case "RFT":
  //    return DocumentType.FamilyTemplate;

  //   default:
  //    return DocumentType.Unknown;
  //  }
  // }
  //}

  #endregion

  #region Private Properties

  private StorageInfo StorageRoot { get; set; }

  #endregion

  #region Public Methods


  #endregion

  #region Protected Methods

  #endregion

  #region Private Methods

  private void OpenStructuredStorageFile()
  {
   //int checkResult = IsStorageFile(FileName);
   IsInitialized = false;
   //if(checkResult == 0)
   //{
   // return;
   //}

   try
   {
    StorageRoot = GetStorageRoot(FileName);
   }
   catch(Exception ex)
   {
    Debug.WriteLine(ex.Message);
   }
  }

  //private int IsStorageFile(string fileName)
  //{
  // return Ole32.StgIsStorageFile(FileName);
  //}

  private StorageInfo GetStorageRoot(string fileName)
  {
   try
   {
    StorageInfo storageRoot = (StorageInfo)InvokeStorageRootMethod(null,
                                                                   "Open",
                                                                   fileName,
                                                                   FileMode.Open,
                                                                   FileAccess.Read,
                                                                   FileShare.Read);
    if(storageRoot == null)
    {
     IsInitialized = false;
     throw new Exception(string.Format("Unable to open \"{0}\" as a structured storage file.", fileName));
    }
    IsInitialized = true;
    return storageRoot;
   }
   catch (Exception ex)
   {
    IsInitialized = false;
    Debug.WriteLine(ex.Message);
   }
   return null;
  }

  private void CloseStorageRoot(StorageInfo storageRoot)
  {
   InvokeStorageRootMethod(storageRoot, "Close");
  }

  #endregion

 }
}
