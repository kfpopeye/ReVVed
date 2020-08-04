
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Packaging;
using System.Text;

namespace RVVD.StructuredStorage
{

 public class BasicFileInfo : StorageStreamBase
 {

  #region Private Variables

  private WorkSharingMode _workSharing = WorkSharingMode.Unknown;
  private string _userName = string.Empty;
  private string _centralFilePath = string.Empty;
  private string _revitBuild = string.Empty;
  private string _lastSavedpath = string.Empty;
  private int _openWorksetDefault = 0;

  #endregion

  #region Constructors

  public BasicFileInfo(string fileName, StorageInfo storage)
   : base(fileName, storage)
  {
   ReadStructuredStorageFile();
  }

  #endregion

  #region Public Properties

  public bool IsCentralFile
  {
   get
   {
    if((WorkSharing == WorkSharingMode.NotEnabled) ||
       (WorkSharing == WorkSharingMode.Unknown))
    {
     return false;
    }
    bool fileNamesMatch = false;
    bool workSharing = WorkSharing == WorkSharingMode.Central;
    string fileName = Path.GetFileName(FileName).ToUpper();
    string centralFile = string.Empty;
    if(CentralFilePath.Length > 0)
    {
     centralFile = Path.GetFileName(CentralFilePath).ToUpper();
    }
    if(centralFile.Length > 0)
    {
     fileNamesMatch = centralFile.Equals(fileName);
    }
    return workSharing && fileNamesMatch;
   }
  }

  public bool IsLocalWorkingFile
  {
   get
   {
    if((WorkSharing == WorkSharingMode.NotEnabled) ||
       (WorkSharing == WorkSharingMode.Unknown))
    {
     return false;
    }
    bool fileNamesMatch = false;
    bool workSharing = WorkSharing == WorkSharingMode.Local;
    string fileName = Path.GetFileName(FileName).ToUpper();
    string centralFile = string.Empty;
    if(CentralFilePath.Length > 0)
    {
     centralFile = Path.GetFileName(CentralFilePath).ToUpper();
    }
    if(centralFile.Length > 0)
    {
     fileNamesMatch = centralFile.Equals(fileName);
    }
    return workSharing || (fileNamesMatch == false);
   }
  }

  public WorkSharingMode WorkSharing
  {
   get
   {
    return _workSharing;
   }
   private set
   {
    _workSharing = value;
   }
  }

  public string UserName
  {
   get
   {
    return _userName;
   }
   private set
   {
    _userName = value;
   }
  }

  public string CentralFilePath
  {
   get
   {
    return _centralFilePath;
   }
   private set
   {
    _centralFilePath = value;
   }
  }

  public string RevitBuild
  {
   get
   {
    return _revitBuild;
   }
   private set
   {
    _revitBuild = value;
   }
  }

  public ProductType Product
  {
   get
   {
    if(string.IsNullOrEmpty(RevitBuild))
    {
     return ProductType.Unknown;
    }

    if(RevitBuild.ToUpper().IndexOf("MEP") >= 0)
    {
     return ProductType.MEP;
    }

    if(RevitBuild.ToUpper().IndexOf("ARCHITECTURE") >= 0)
    {
     return ProductType.Architecture;
    }

    if(RevitBuild.ToUpper().IndexOf("STRUCTURE") >= 0)
    {
     return ProductType.Structure;
    }

    return ProductType.Unknown;
   }
  }

  public string BuildTimeStamp
  {
   get
   {
    if(string.IsNullOrEmpty(RevitBuild))
    {
     return string.Empty;
    }

    string[] buildParts = RevitBuild.Split(new char[] { ':' });
    if(buildParts != null)
    {
     if(buildParts.Length == 2)
     {
      string timeStamp = buildParts[1].Trim().Replace("(x64))", string.Empty);
      timeStamp = timeStamp.Replace("(x64)", string.Empty);
      timeStamp = timeStamp.Replace(")", string.Empty);
      return timeStamp.Trim();
     }
    }

    return _revitBuild.Trim();
   }
  }

  public PlatformType Platform
  {
   get
   {
    if(string.IsNullOrEmpty(RevitBuild))
    {
     return PlatformType.Unknown;
    }

    if(RevitBuild.ToUpper().IndexOf("X64") >= 0)
    {
     return PlatformType.x64;
    }

    return PlatformType.x86;
   }
  }

  public string LastSavedpath
  {
   get
   {
    return _lastSavedpath;
   }
   private set
   {
    _lastSavedpath = value;
   }
  }

  public int OpenWorksetDefault
  {
   get
   {
    return _openWorksetDefault;
   }
   private set
   {
    _openWorksetDefault = value;
   }
  }

  #endregion

  #region Private Properties


  #endregion

  #region Public Methods


  #endregion

  #region Private Methods

  private void ParseDetailInfo(string detailInfo)
  {
   detailInfo = detailInfo.Trim();
   int index = detailInfo.IndexOf(":");
   string detailValue = detailInfo.Substring(detailInfo.IndexOf(":") + 1);
   string detailKey = detailInfo.Substring(0, detailInfo.IndexOf(":"));
   detailKey = detailKey.Trim().ToUpper().Replace(" ", string.Empty);
   detailKey = StringUtility.PurgeUnprintableCharacters(detailKey);
   detailValue = StringUtility.PurgeUnprintableCharacters(detailValue);

   switch(detailKey)
   {
    case "WORKSHARING":
     if(string.IsNullOrEmpty(detailValue))
     {
      WorkSharing = WorkSharingMode.Unknown;
      return;
     }

     string workSharing = detailValue.Replace(" ", string.Empty).Trim().ToUpper();
     switch(workSharing)
     {
      case "NOTENABLED":
       WorkSharing = WorkSharingMode.NotEnabled;
       break;

      case "LOCAL":
       WorkSharing = WorkSharingMode.Local;
       break;

      case "CENTRAL":
       WorkSharing = WorkSharingMode.Central;
       break;

      default:
       WorkSharing = WorkSharingMode.Unknown;
       break;
     }
     break;

    case "USERNAME":
     UserName = detailValue.Trim();
     break;

    case "CENTRALFILEPATH":
     CentralFilePath = detailValue.Trim();
     break;

    case "REVITBUILD":
     RevitBuild = detailValue.Trim();
     break;

    case "LASTSAVEPATH":
     LastSavedpath = detailValue.Trim();
     break;

    case "OPENWORKSETDEFAULT":
     OpenWorksetDefault = Convert.ToInt32(detailValue.Trim());
     break;

    default:
     //Debug.Assert(false, string.Format("{0} was not found in the case tests.", detailKey));
     break;
   }
  }

  #endregion

  #region Protected Methods

  //internal override void ReadStructuredStorageFile()
  //{
  // if(IsInitialized)
  // {
  //  return;
  // }
  // try
  // {
  //  StreamInfo[] streams = Storage.GetStreams();
  //  foreach(StreamInfo stream in streams)
  //  {
  //   if(stream.Name.ToUpper().Equals("BASICFILEINFO"))
  //   {
  //    string unicodeData = StringUtility.ConvertStreamBytesToUnicode(stream);
  //    string[] basicFileInfoParts = unicodeData.Split(new char[] { '\0' });

  //    foreach(string basicFileInfoPart in basicFileInfoParts)
  //    {
  //     if(basicFileInfoPart.IndexOf("\r\n") >= 0)
  //     {
  //      string[] detailInfoParts = basicFileInfoPart.Split(new string[] { "\r\n" }, new StringSplitOptions());
  //      foreach(string detailPart in detailInfoParts)
  //      {
  //       ParseDetailInfo(detailPart);
  //      }
  //     }
  //    }
  //   }
  //  }
  // }
  // catch(Exception ex)
  // {
  //  LogManager.LogMessage(ex);
  //  IsInitialized = false;
  // }
  // IsInitialized = true;
  //}

  public void ReadStructuredStorageFile()
  {
   if(IsInitialized)
   {
    return;
   }
   try
   {
    StreamInfo[] streams = Storage.GetStreams();
    foreach(StreamInfo stream in streams)
    {
     if(stream.Name.ToUpper().Equals("BASICFILEINFO"))
     {
      string unicodeData = StringUtility.ConvertStreamBytesToUnicode(stream);
      string[] basicFileInfoParts = unicodeData.Split(new char[] { '\0' });

      foreach(string basicFileInfoPart in basicFileInfoParts)
      {
       if(basicFileInfoPart.IndexOf("\r\n") >= 0)
       {
        string[] detailInfoParts = basicFileInfoPart.Split(new string[] { "\r\n" }, new StringSplitOptions());
        foreach(string detailPart in detailInfoParts)
        {
         ParseDetailInfo(detailPart);
        }
       }
      }
     }
    }
   }
   catch(Exception ex)
   {
    Debug.WriteLine(ex.Message);
    IsInitialized = false;
   }
   IsInitialized = true;
  }

  public override string ToString()
  {
   StringBuilder sb = new StringBuilder();
   string seperator = string.Empty;
   try
   {
    if(this != null)
    {
     seperator = "+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+" + Environment.NewLine;
     sb.Append(string.Format("FileName: <{0}>{1}", FileName, Environment.NewLine));
     sb.Append(seperator);
     sb.Append(string.Format("BasicFileInfo Section{0}", Environment.NewLine));
     sb.Append(seperator);
     sb.Append(string.Format("DocType: <{0}>{1}", DocType, Environment.NewLine));
     sb.Append(string.Format("WorkSharing: <{0}>{1}", WorkSharing, Environment.NewLine));
     sb.Append(string.Format("IsCentralFile: <{0}>{1}", IsCentralFile, Environment.NewLine));
     sb.Append(string.Format("UserName: <{0}>{1}", UserName, Environment.NewLine));
     sb.Append(string.Format("CentralFilePath: <{0}>{1}", CentralFilePath, Environment.NewLine));
     sb.Append(string.Format("RevitBuild: <{0}>{1}", RevitBuild, Environment.NewLine));
     sb.Append(string.Format("Product: <{0}>{1}", Product, Environment.NewLine));
     sb.Append(string.Format("Platform: <{0}>{1}", Platform, Environment.NewLine));
     sb.Append(string.Format("BuildTimeStamp: <{0}>{1}", BuildTimeStamp, Environment.NewLine));
     sb.Append(string.Format("LastSavedpath: <{0}>{1}", LastSavedpath, Environment.NewLine));
     sb.Append(string.Format("OpenWorksetDefault: <{0}>{1}", OpenWorksetDefault, Environment.NewLine));
     sb.Append(seperator);
     seperator = sb.ToString();
     return seperator;
    }
   }
   finally
   {
    sb.Length = 0;
    sb.Capacity = 0;
    sb = null;
   }
   return string.Empty;
  }

  #endregion
 }

}
