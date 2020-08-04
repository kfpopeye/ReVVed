

using System;
using System.IO;
using System.IO.Packaging;
using System.Text;

namespace RVVD
{
 
 /// <summary>
 /// Utility for manipulating string objects.
 /// </summary>
 public sealed class StringUtility
 {

  // added to suppress the default public constructor
  private StringUtility()
  {
  }

  /// <summary>
  /// Adds a trailing backslash (\) to the supplied
  /// path if it does not exist.
  /// </summary>
  /// <param name="path">The path to check.</param>
  /// <returns>
  /// The supplied path is returned with a trailing 
  /// backslash (\) if is does not exist. If path is 
  /// an empty string, an empty string is returned.
  /// </returns>
  public static string AddTrailingBackslash(string path)
  {
   // check for null argument
   if(path == null)
   {
    throw new ArgumentNullException("path");
   }

   // check for empty string
   if(path.Length == 0)
   {
    return path;
   }

   // check for existing backslash
   if(path.EndsWith(@"\") == false)
   {
    return string.Format(@"{0}\", path);
   }

   // there is already a trailing backslash
   return path;
  }

  /// <summary>
  /// Counts the number of specified characters in the specified string.
  /// </summary>
  /// <param name="value">The string to parse.</param>
  /// <param name="characterToCount">The character to count.</param>
  /// <returns>This method returns the number of specified characters
  /// in the specified string. Zero (0) is returned if the specified
  /// string does not contain any of the specified characters.</returns>
  public static int CharacterCount(string value, char characterToCount)
  {
   int count = 0;
   for(int i = 0; i < value.Length; i++)
   {
    if(value[i].Equals(characterToCount))
    {
     count += 1;
    }
   }
   return count;
  }

  /// <summary>
  /// 
  /// </summary>
  /// <param name="value">The original string the substring will
  /// be taken from.</param>
  /// <param name="startIndex">The starting index (zero based) in the
  /// original string to begin the search for the delimiter character.</param>
  /// <param name="length">The number of characters from the starting index
  /// to search for the delimiter.</param>
  /// <param name="delimiter">The delimiter used to find the end of the substring.</param>
  /// <param name="count">The number of occurances of the delimiter to use to
  /// find the end of the substring.</param>
  /// <returns>This method returns the portion of the original string that contains
  /// count occurrances of the specified delimiter. The search starts at the character
  /// in the startingIndex position and continues for length characters.</returns>
  /// <remarks>
  /// The string returned by this method includes the nth occurance of the
  /// sprcified delimiter.<br/>
  /// Given the following string (originalString)...<br/>
  /// P:\Projects\546002\Central Campus\Sheets\Buildings 3-4-5\HVAC\<br/>
  /// SubString(originalString, 3, 31, '\', 1) returns "Projects\"<br/>
  /// SubString(originalString, 3, 31, '\', 2) returns "Projects\546002\"<br/>
  /// SubString(originalString, 3, 31, '\', 3) returns "Projects\546002\Central Campus\"<br/>
  /// SubString(originalString, 3, 31, '\', 7) returns "Projects\546002\Central Campus\Sheets\Buildings 3-4-5\HVAC\"<br/>
  /// SubString(originalString, 3, 31, '_', 3) returns the original string<br/>
  /// </remarks>
  public static string Substring(string value, int startIndex, int length, char delimiter, int count)
  {
   if(count == 0)
   {
    return value;
   }

   if(CharacterCount(value, delimiter) < count)
   {
    return value;
   }

   int internalCount = 0;

   for(int i = startIndex; i < length; i++)
   {
    if(value[i].Equals(delimiter))
    {
     internalCount += 1;
    }
    if(internalCount == count)
    {
     return value.Substring(startIndex, i + 1);
    }
   }

   return value;
  }

  /// <summary>
  /// Finds a substring in the entire specified original string using the
  /// specified count occurrance of the specified delimiter.
  /// </summary>
  /// <param name="value">The original string the substring will
  /// be taken from.</param>
  /// <param name="delimiter">The delimiter used to find the end of the substring.</param>
  /// <param name="count">The number of occurances of the delimiter to use to
  /// find the end of the substring.</param>
  /// <returns>This method returns the portion of the original string that contains
  /// count occurrances of the specified delimiter.</returns>
  /// <remarks>
  /// The string returned by this method includes the nth occurance of the
  /// sprcified delimiter.<br/>
  /// Given the following string (originalString)...<br/>
  /// P:\Projects\546002\Central Campus\Sheets\Buildings 3-4-5\HVAC\<br/>
  /// SubString(originalString, '\', 2) returns "P:\Projects\"<br/>
  /// SubString(originalString, '\', 4) returns "P:\Projects\546002\Central Campus\"<br/>
  /// SubString(originalString, '\', 7) returns "P:\Projects\546002\Central Campus\Sheets\Buildings 3-4-5\HVAC\"<br/>
  /// SubString(originalString, '\', 9) returns the original string<br/>
  /// SubString(originalString, '_', 9) returns the original string<br/>
  /// </remarks>
  public static string Substring(string value, char delimiter, int count)
  {
   int startIndex = 0;
   int length = value.Length - 1;
   return Substring(value, startIndex, length, delimiter, count);
  }

  /// <summary>
  /// Purges unprintable characters from the supplied string.
  /// </summary>
  /// <param name="oldValue">The string value that will be purged.</param>
  /// <returns>The supplied value is returned as an ASCII string with all
  /// characters less than Decimnal 32 (SPACE) and greater than Decimal 126
  /// (~) removed.</returns>
  /// <remarks>The characters out of range are not replaced with a printable
  /// character. They are permanently removed.</remarks>
  public static string PurgeUnprintableCharacters(string oldValue)
  {
   StringBuilder sb = new StringBuilder();
   char[] oldValueArray = oldValue.ToCharArray();
   foreach(char letter in oldValueArray)
   {
    int decimalValue = (int)letter;
    if((decimalValue >= 32) && (decimalValue <= 126))
    {
     sb.Append(letter);
    }
   }
   oldValue = sb.ToString();
   sb.Length = 0;
   sb.Capacity = 0;
   sb = null;
   return oldValue;
  }

  /// <summary>
  /// Converts the byte data from a StreamInfo Package stream to
  /// HEX string data.
  /// </summary>
  /// <param name="streamInfo">The StreamInfo Package the data is read from.</param>
  /// <returns>A HEX string of the byte data is returned.</returns>
  /// <remarks>The underlying stream is opened in Read Only mode using a
  /// Stream object.</remarks>
  public static string ConvertStreamBytesToHex(StreamInfo streamInfo)
  {
   StringBuilder sb = new StringBuilder();
   string convertedValue = string.Empty;
   try
   {
    using(Stream streamReader = streamInfo.GetStream(FileMode.Open, FileAccess.Read))
    {
     int currentRead;
     while((currentRead = streamReader.ReadByte()) >= 0)
     {
      byte currentByte = (byte)currentRead;
      sb.AppendFormat("{0:X2}", currentByte);
     }
     convertedValue = sb.ToString();
     return convertedValue;
    }
   }
   catch (Exception ex)
   {
    throw ex;
   }
   finally
   {
    sb.Length = 0;
    sb.Capacity = 0;
    sb = null;
   }
  }

  /// <summary>
  /// Converts the byte data from a StreamInfo Package stream to
  /// ASCII string data.
  /// </summary>
  /// <param name="streamInfo">The StreamInfo Package the data is read from.</param>
  /// <returns>A ASCII string of the byte data is returned.</returns>
  /// <remarks>The underlying stream is opened in Read Only mode using a
  /// Stream object.</remarks>
  public static string ConvertStreamBytesToASCII(StreamInfo streamInfo)
  {
   byte[] streamData = null;
   try
   {
    using(Stream streamReader = streamInfo.GetStream(FileMode.Open, FileAccess.Read))
    {
     streamData = new byte[streamReader.Length];
     streamReader.Read(streamData, 0, streamData.Length);
     return ASCIIEncoding.ASCII.GetString(streamData);
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

  /// <summary>
  /// Converts the byte data from a StreamInfo Package stream to
  /// Unicode string data.
  /// </summary>
  /// <param name="streamInfo">The StreamInfo Package the data is read from.</param>
  /// <returns>A Unicode string of the byte data is returned.</returns>
  /// <remarks>The underlying stream is opened in Read Only mode using a
  /// Stream object.</remarks>
  public static string ConvertStreamBytesToUnicode(StreamInfo streamInfo)
  {
   byte[] streamData = null;
   try
   {
    using(Stream streamReader = streamInfo.GetStream(FileMode.Open, FileAccess.Read))
    {
     streamData = new byte[streamReader.Length];
     streamReader.Read(streamData, 0, streamData.Length);
     return ASCIIEncoding.Unicode.GetString(streamData);
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


 }
}
