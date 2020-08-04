
using System;
using System.IO;
using System.IO.Packaging;
using System.Reflection;

namespace RVVD.StructuredStorage
{
 
 public abstract class StorageBase
 {

  #region Private Variables

  private string _fileName = string.Empty;
  private bool _isInitialized = false;

  #endregion

  #region Constructors

  public StorageBase(string fileName)
  {
   if(File.Exists(fileName) == false)
   {
    throw new FileNotFoundException(string.Format("The file \"{0}\" was not found.", fileName));
   }
   FileName = fileName;
  }


  #endregion

  #region Public Properties

  public bool IsInitialized
  {
   get
   {
    return _isInitialized;
   }
   protected set
   {
    _isInitialized = value;
   }
  }

  public string FileName
  {
   get
   {
    return _fileName;
   }
   protected set
   {
    _fileName = value;
   }
  }

  public string FileExtension
  {
   get
   {
    if(string.IsNullOrEmpty(FileName))
    {
     return string.Empty;
    }

    return Path.GetExtension(FileName).Replace(".", string.Empty);
   }
  }

  public DocumentType DocType
  {
   get
   {
    if(string.IsNullOrEmpty(FileExtension))
    {
     return DocumentType.Unknown;
    }

    switch(FileExtension.ToUpper())
    {
     case "RVT":
      return DocumentType.Project;

     case "RTE":
      return DocumentType.ProjectTemplate;

     case "RFA":
      return DocumentType.Family;

     case "RFT":
      return DocumentType.FamilyTemplate;

     default:
      return DocumentType.Unknown;
    }
   }
  }


  #endregion

  #region Private Properties


  #endregion

  #region Public Methods


  #endregion

  #region Private Methods

  #endregion

  #region Protected Methods

  protected object InvokeStorageRootMethod(StorageInfo storageRoot,
                                           string methodName,
                                           params object[] methodArgs)
  {
   //We need the StorageRoot class to directly open an OSS file.  Unfortunately, it's internal.
   //So we'll have to use Reflection to access it.  This code was inspired by:
   //http://henbo.spaces.live.com/blog/cns!2E073207A544E12!200.entry
   //Note: In early WinFX CTPs the StorageRoot class was public because it was documented
   //here: http://msdn2.microsoft.com/en-us/library/aa480157.aspx

   BindingFlags bindingFlags = BindingFlags.Static |
                               BindingFlags.Instance |
                               BindingFlags.Public |
                               BindingFlags.NonPublic |
                               BindingFlags.InvokeMethod;

   Type storageRootType = typeof(StorageInfo).Assembly.GetType("System.IO.Packaging.StorageRoot",
                                                               true,
                                                               false);
   //try
   //{
    object result = storageRootType.InvokeMember(methodName,
                                                 bindingFlags,
                                                 null,
                                                 storageRoot,
                                                 methodArgs);

    return result;
   //}
   //catch (Exception ex)
   //{
    
   // throw;
   //}
   ////return null;
  }

  #endregion

 }

}
