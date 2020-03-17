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
// Modified On:  2020/03/16 02:17
// Modified By:  Alexis

#endregion




using Microsoft.Build.Framework;
using MSBuild.Tools.Helpers;

namespace MSBuild.Tools.Tasks.Git
{
  /// <summary>Attempts to get the Release Notes for a given Git Tag</summary>
  public class GitCreateReleaseNotes : GitTaskBase
  {
    #region Properties & Fields - Public

    /// <summary>The version from which to start compiling commits' descriptions</summary>
    [Required]
    public string FromVersion { get; set; }

    /// <summary>The version until which to compile commits' descriptions, defaults to origin/HEAD</summary>
    public string ToVersion { get; set; } = "refs/remotes/origin/HEAD";

    /// <summary>The format to apply, defaults to format:%B (the commit message)</summary>
    public string Format { get; set; } = "format:%B";

    /// <summary>Optional git branch, defaults to HEAD</summary>
    public string Branch { get; set; } = "HEAD";

    /// <summary>Optional git remote, defaults to origin</summary>
    public string Remote { get; set; } = "origin";

    /// <summary>The tag's release note (if any)</summary>
    [Output]
    public string ReleaseNotes { get; private set; } = string.Empty;

    /// <summary>The resolved from version (in case it is a variable)</summary>
    [Output]
    public string ResolvedFromVersion { get; set; }

    /// <summary>The resolved to version (in case it is a variable)</summary>
    [Output]
    public string ResolvedToVersion { get; set; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool ExecuteTask()
    {
      ResolvedFromVersion = this.ExpandToCommitHash(FromVersion, Branch, Remote);
      ResolvedToVersion   = this.ExpandToCommitHash(ToVersion, Branch, Remote);

      ReleaseNotes = this.ConcatCommitInfo(ResolvedFromVersion, ResolvedToVersion, Format, Remote, Branch);

      return true;
    }

    #endregion
  }
}
