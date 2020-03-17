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
// Modified On:  2020/03/15 20:02
// Modified By:  Alexis

#endregion




using Microsoft.Build.Framework;

namespace MSBuild.Tools.Tasks.Git
{
  /// <summary>Attempts to get the Release Notes for a given Git Tag</summary>
  public class GitGetTagMessage : GitTaskBase
  {
    #region Properties & Fields - Public

    /// <summary>The git tag version</summary>
    [Required]
    public string TagVersion { get; set; }

    /// <summary>Optional git remote, default to "origin"</summary>
    public string Remote { get; set; } = "origin";

    /// <summary>Whether to fetch the tag or not, default to true</summary>
    public bool FetchTag { get; set; } = true;

    /// <summary>The tag's release note (if any)</summary>
    [Output]
    public string ReleaseNote { get; private set; } = string.Empty;

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool ExecuteTask()
    {
      if (FetchTag)
        ExecuteGit($"fetch {Remote} refs/tags/{TagVersion}:refs/tags/{TagVersion}",
                   $"No git tag found for version {TagVersion}, release note will be empty.",
                   false);

      ReleaseNote = ExecuteGit($"tag -l --format=\"%(contents)\" {TagVersion}",
                               $"An error occurred while retrieving the git tag {TagVersion}: '{{1}}'.",
                               false);

      return true;
    }

    #endregion
  }
}
