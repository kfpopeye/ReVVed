
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Packaging;
using System.Windows.Media.Imaging;

namespace RVVD.StructuredStorage
{

 public class PreviewImage : StorageStreamBase
 {

  #region Private Variables

  private byte[] _previewData = null;

  #endregion

  #region Constructors

  public PreviewImage(string fileName, StorageInfo storage)
   : base(fileName, storage)
  {
   ReadStructuredStorageFile();
  }

  #endregion

  #region Public Properties


  #endregion

  #region Private Properties

  private byte[] PreviewData
  {
   get
   {
    return _previewData;
   }
   set
   {
    _previewData = value;
   }
  }

  #endregion

  #region Public Methods

  public Image GetPreviewAsImage()
  {
   if((PreviewData == null) || (PreviewData.Length <= 0))
   {
    using(Bitmap newBitmap = new Bitmap(128, 128))
    {
     return newBitmap.Clone() as Bitmap;
    }
   }

   // read past the Revit metadata to the start of the PNG image
   int startingOffset = GetPngStartingOffset();
   if(startingOffset == 0)
   {
    using(Bitmap newBitmap = new Bitmap(128, 128))
    {
     return newBitmap.Clone() as Bitmap;
    }
   }

   try
   {
    byte[] pngDataBuffer = new byte[PreviewData.GetUpperBound(0) - startingOffset + 1];
    // read the PNG image data into a byte array
    using(MemoryStream ms = new MemoryStream(PreviewData))
    {
     ms.Position = startingOffset;
     ms.Read(pngDataBuffer, 0, pngDataBuffer.Length);
    }

    byte[] decoderData = null;

    // if the image data is valid
    if(pngDataBuffer != null)
    {
     // use a memory stream to decode the PNG image data
     // and copy the decoded data into a byte array
     using(MemoryStream ms = new MemoryStream(pngDataBuffer))
     {
      PngBitmapDecoder decoder = new PngBitmapDecoder(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
      decoderData = BitSourceToArray(decoder.Frames[0]);
     }
    }

    // if the decoded data is valie
    if((decoderData != null) && (decoderData.Length > 0))
    {
     // use another memory stream to create a Bitmap
     // and then an Image from that Bitmap
     using(MemoryStream ms = new MemoryStream(decoderData))
     {
      using(Bitmap newBitmap = new Bitmap((ms)))
      {
       using(Image newImage = newBitmap)
       {
        return newImage.Clone() as Image;
       }
      }
     }
    }
   }
   catch (Exception ex)
   {
    Debug.WriteLine(ex.Message);
   }

   using(Bitmap newBitmap = new Bitmap(128, 128))
   {
    return newBitmap.Clone() as Bitmap;
   }
  }

  #endregion

  #region Private Methods

  private byte[] BitSourceToArray(BitmapSource bitmapSource)
  {
   BitmapEncoder encoder = new JpegBitmapEncoder();
   using(MemoryStream ms = new MemoryStream())
   {
    encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
    encoder.Save(ms);
    return ms.ToArray();
   }
  }

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
  //   if(stream.Name.ToUpper().Equals("REVITPREVIEW4.0"))
  //   {
  //    PreviewData = ParsePreviewInfo(stream);
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
     if(stream.Name.ToUpper().Equals("REVITPREVIEW4.0"))
     {
      PreviewData = ParsePreviewInfo(stream);
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

  private byte[] ParsePreviewInfo(StreamInfo streamInfo)
  {
   byte[] streamData = null;
   try
   {
    using(Stream streamReader = streamInfo.GetStream(FileMode.Open, FileAccess.Read))
    {
     streamData = new byte[streamReader.Length];
     streamReader.Read(streamData, 0, streamData.Length);
     return streamData;
    }
   }
   catch(Exception ex)
   {
    throw ex;
   }
   finally
   {
    streamData = null;
   }
  }

  private int GetPngStartingOffset()
  {
   bool markerFound = false;
   int startingOffset = 0;
   int previousValue = 0;
   using(MemoryStream ms = new MemoryStream(PreviewData))
   {
    for (int i = 0; i < PreviewData.Length; i++)
    {
     int currentValue = ms.ReadByte();
     // possible start of PNG file data
     if(currentValue == 137)   // 0x89
     {
      markerFound = true;
      startingOffset = i;
      previousValue = currentValue;
      continue;
     }

     switch(currentValue)
     {
      case 80:   // 0x50
       if(markerFound && (previousValue == 137))
       {
        previousValue = currentValue;
        continue;
       }
       markerFound = false;
       break;

      case 78:   // 0x4E
       if(markerFound && (previousValue == 80))
       {
        previousValue = currentValue;
        continue;
       }
       markerFound = false;
       break;

      case 71:   // 0x47
       if(markerFound && (previousValue == 78))
       {
        previousValue = currentValue;
        continue;
       }
       markerFound = false;
       break;

      case 13:   // 0x0D
       if(markerFound && (previousValue == 71))
       {
        previousValue = currentValue;
        continue;
       }
       markerFound = false;
       break;

      case 10:   // 0x0A
       if(markerFound && (previousValue == 26))
       {
        return startingOffset;
       }
       if(markerFound && (previousValue == 13))
       {
        previousValue = currentValue;
        continue;
       }
       markerFound = false;
       break;

      case 26:   // 0x1A
       if(markerFound && (previousValue == 10))
       {
        previousValue = currentValue;
        continue;
       }
       markerFound = false;
       break;
     }
    }
   }
   return 0;
  }

  #endregion

 }
}
