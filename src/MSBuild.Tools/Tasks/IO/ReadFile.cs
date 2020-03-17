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
// Modified On:  2020/03/15 19:59
// Modified By:  Alexis

#endregion




using System.IO;
using Microsoft.Build.Framework;
using MSBuild.Tools.Helpers;

namespace MSBuild.Tools.Tasks.IO
{
  public class ReadFile : TaskBase
  {
    #region Properties & Fields - Public

    /// <summary>The file path to read</summary>
    [Required]
    public string FilePath { get; set; }

    /// <summary>Optional default value to return if the file doesn't exist, defaults to empty string</summary>
    public string Default { get; set; } = string.Empty;

    /// <summary>Optionally fail task if the file doesn't exist, defaults to false</summary>
    public bool FailIfMissing { get; set; } = false;

    /// <summary>The file's content or <see cref="Default" /> if the file doesn't exist</summary>
    [Output]
    public string Content { get; private set; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool ExecuteTask()
    {
      if (File.Exists(FilePath) == false)
      {
        Content = Default;
        return FailIfMissing == false;
      }

      Content = File.ReadAllText(FilePath);
      return true;
    }

    #endregion
  }
}
