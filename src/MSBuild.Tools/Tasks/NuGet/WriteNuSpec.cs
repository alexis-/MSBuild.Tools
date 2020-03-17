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
// Modified On:  2020/03/15 20:08
// Modified By:  Alexis

#endregion




using Microsoft.Build.Framework;
using MSBuild.Tools.Helpers;

namespace MSBuild.Tools.Tasks.NuGet
{
  /// <summary>Adds or replace properties in a NuSpec file</summary>
  public class WriteNuSpec : TaskBase
  {
    #region Properties & Fields - Public

    /// <summary>The .nuspec file path</summary>
    [Required]
    public string NuSpecFilePath { get; set; }

    /// <summary>
    ///   Optional editing mode: Append, AppendLine, Insert, InsertLine, Replace. Unknown value
    ///   is Replace. Defaults to Replace.
    /// </summary>
    public string EditMode { get; set; }

    /// <summary>
    ///   An array of property and value to add or edit. Mutually exclusive with
    ///   <see cref="NuSpecProperty" /> and <see cref="NuSpecValue" />
    /// </summary>
    public ITaskItem[] NuSpecPropValues { get; set; }

    /// <summary>The property to add or edit. Mutually exclusive with <see cref="NuSpecPropValues" />.</summary>
    public string NuSpecProperty { get; set; }

    /// <summary>The value to set. Mutually exclusive with <see cref="NuSpecPropValues" />.</summary>
    public string NuSpecValue { get; set; }

    /// <summary>The NuSpec section to modify, defaults to metadata</summary>
    public string NuSpecSection { get; set; } = "metadata";

    /// <summary>Whether to fail the task if the inputs are empty. Defaults to true.</summary>
    public bool FailIfEmpty { get; set; } = true;

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool ExecuteTask()
    {
      return this.WriteNuSpec(
        NuSpecFilePath,
        EditMode,
        NuSpecPropValues,
        NuSpecProperty,
        NuSpecValue,
        NuSpecSection,
        FailIfEmpty);
    }

    #endregion
  }
}
