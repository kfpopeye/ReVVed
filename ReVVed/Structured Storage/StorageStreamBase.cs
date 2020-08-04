using System.IO.Packaging;

namespace RVVD.StructuredStorage
{

 public abstract class StorageStreamBase : StorageBase 
 {

  #region Private Variables


  #endregion

  #region Constructors

  public StorageStreamBase(string fileName, StorageInfo storage) 
   : base(fileName)
  {
   FileName = fileName;
   Storage = storage;
  }


  #endregion

  #region Protected Methods

  ///// <summary>
  ///// Abstract method that must be overriden for each type of
  ///// structured storage we will read.
  ///// </summary>
  ///// <param name="fileName">The file name we want to read.</param>
  //internal abstract void ReadStructuredStorageFile();

  #endregion

  #region Public Properties

  public StorageInfo Storage { get; set; }

  #endregion

  #region Private Properties


  #endregion

  #region Public Methods


  #endregion

  #region Private Methods


  #endregion


 }

}
