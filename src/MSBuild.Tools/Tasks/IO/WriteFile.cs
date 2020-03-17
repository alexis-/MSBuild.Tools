#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Modified On:  2020/03/14 17:11
// Modified By:  Alexis

#endregion




using System.IO;
using Microsoft.Build.Framework;
using MSBuild.Tools.Helpers;

namespace MSBuild.Tools.Tasks.IO
{
  /// <summary>
  /// Advanced file writing
  /// </summary>
  public class WriteFile : TaskBase
  {
    #region Properties & Fields - Public

    /// <summary>The file path to write</summary>
    [Required]
    public string FilePath { get; set; }

    /// <summary>The content to write</summary>
    public string Content { get; set; }

    /// <summary>
    ///   Optional editing mode: Append, AppendLine, Insert, InsertLine, Overwrite, Replace. Unknown value
    ///   is Overwrite. Defaults to Overwrite.
    /// </summary>
    public string EditMode { get; set; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool ExecuteTask()
    {
      if (string.IsNullOrWhiteSpace(Content))
        return true;

      if (File.Exists(FilePath) == false)
      {
        if (EditMode == "Replace")
        {
          LogError($"File {FilePath} doesn't exist and edit mode is Replace. Make sure the file exists or change the edit mode.");
          return false;
        }

        File.WriteAllText(FilePath, Content);
        return true;
      }
      
      using (var fs = File.Open(FilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
      {
        string tmpContent = string.Empty;

        if (EditMode != "Replace")
          using (var reader = new StreamReader(fs))
            tmpContent = reader.ReadToEnd();

        using (var writer = new StreamWriter(fs))
        {
          switch (EditMode)
          {
            case "Append":
              tmpContent += Content;
              break;

            case "AppendLine":
              tmpContent += "\\n" + Content;
              break;

            case "Insert":
              tmpContent = Content + tmpContent;
              break;

            case "InsertLine":
              tmpContent = Content + "\\n" + tmpContent;
              break;

            case "Replace":
            default:
              tmpContent = Content;
              break;
          }

          writer.Write(tmpContent);
        }
      }

      return true;
    }

    #endregion
  }
}
