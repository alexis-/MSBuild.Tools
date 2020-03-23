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
// Modified On:  2020/03/18 23:53
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using MSBuild.Tools.Exceptions;
using MSBuild.Tools.Extensions;
using MSBuild.Tools.Helpers;
using MSBuild.Tools.Models;

namespace MSBuild.Tools.Tasks.Git
{
  /// <summary>
  ///   Attempts to automatically build a ChangeLog and optional Release notes for the NuSpec
  ///   file from git commits' messages
  /// </summary>
  public class GitCreateChangeLog : GitTaskBase
  {
    #region Constants & Statics

    private static readonly Regex RE_ChangeLogVersionHeader =
      new Regex(@"^\[(?<Version>Next version|[\w.\-]+)(?: \((?<Commit>[a-f0-9]{40})\))?\]$",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex RE_Indent       = new Regex("^([\\s]*)", RegexOptions.Compiled);
    private static readonly Regex RE_LineCategory = new Regex("^[\\s\\-]*(?<Category>[\\w]+).+$", RegexOptions.Compiled);

    /// <summary>The format to apply (the commit message)</summary>
    private const string Format = "format:%B";

    /// <summary>
    ///   The internal category for content that do not belong to any category, see
    ///   <see cref="ApplyChangeLogStyle(StringBuilder, string)" />
    /// </summary>
    private const string NoCategoryKey = "_noCategory";

    #endregion




    #region Properties & Fields - Public

    /// <summary>The ChangeLog file path</summary>
    [Required]
    public string ChangeLogFilePath { get; set; }

    /// <summary>Optional NuSpec file path to set the <releaseNotes></releaseNotes> element</summary>
    public string NuSpecFilePath { get; set; }

    /// <summary>Whether to try and preserve user edits in the ChangeLog, defaults to false.</summary>
    public bool PreserveChanges { get; set; } = false;

    /// <summary>Optional git branch, defaults to master</summary>
    public string RefSpec { get; set; } = "master";

    /// <summary>
    ///   Optional semi-colon-separated categories to group commit message content together
    ///   (e.g. "Added;Fixed;Misc")
    /// </summary>
    public string Categories { get; set; } = null;

    /// <summary>Whether the current version's release notes contain any content</summary>
    [Output]
    public bool CurrentVersionHasReleaseNotes { get; set; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool ExecuteTask()
    {
      bool               analyzeFile    = File.Exists(ChangeLogFilePath) && PreserveChanges;
      var                versionDescMap = new Dictionary<string, VersionDescription>();
      VersionDescription nextRelDesc    = null;

      RefSpec.DefaultIfWhiteSpaceOrNull("master");

      //
      // Read and analyze existing ChangeLog
      using (var fs = File.Open(ChangeLogFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
        if (analyzeFile)
          versionDescMap = ReadChangeLog(fs, out nextRelDesc);

      //
      // Synchronize Git and ChangeLog
      var tagCommitMap = this.GetTagCommitMap(RefSpec);

      // Remove versions that do not exist in the tag list. That might cause issue when switching between branches
      foreach (var versionName in versionDescMap.Keys
                                                .Where(vn => tagCommitMap.ContainsKey(vn) == false)
                                                .ToArray())
        versionDescMap.Remove(versionName);

      // Define the ordering of our ChangeLog
      foreach (var versionName in versionDescMap.Keys.ToArray())
        versionDescMap[versionName].No = tagCommitMap[versionName].No;

      var noCommitMap = tagCommitMap.Values.ToDictionary(k => k.No);

      // Add missing tags and compose their content
      foreach (var tagName in tagCommitMap.Keys
                                          .Where(tn => versionDescMap.ContainsKey(tn) == false)
                                          .ToArray())
      {
        var tag = tagCommitMap[tagName];
        var prevTag = tag.No > 0
          ? noCommitMap[tag.No - 1]
          : null;

        versionDescMap[tagName] = ComposeExistingVersionDescription(tag, prevTag?.CommitHash);
      }

      //
      // Create the next version release note
      var allVersions        = versionDescMap.Values.ToList();
      var lastTag            = versionDescMap.Values.OrderByDescending(v => v.No).FirstOrDefault();
      var fromCommitExcluded = nextRelDesc?.CommitHash ?? lastTag?.CommitHash;
      var toCommitIncluded   = RefSpec;
      var toCommitExpanded   = this.ExpandToCommitHash(toCommitIncluded);

      if (toCommitExpanded != fromCommitExcluded)
      {
        string releaseNote = this.ConcatCommitInfo(fromCommitExcluded, toCommitIncluded);

        if (nextRelDesc != null && string.IsNullOrWhiteSpace(nextRelDesc.Content.ToString()) == false)
          releaseNote += "\n" + nextRelDesc.Content;

        nextRelDesc = new VersionDescription(VersionDescription.NextVersionName,
                                             toCommitExpanded,
                                             (lastTag?.No ?? 0) + 1);
        nextRelDesc.Content.Append(releaseNote);

        allVersions.Add(nextRelDesc);
      }

      else
        nextRelDesc = lastTag;
      
      CurrentVersionHasReleaseNotes = string.IsNullOrWhiteSpace(nextRelDesc?.Content.ToString()) == false;
        

      //
      // Save ChangeLog
      WriteChangeLog(allVersions);

      //
      // Write NuSpec if required
      if (string.IsNullOrWhiteSpace(NuSpecFilePath) == false && File.Exists(NuSpecFilePath) && nextRelDesc != null)
        this.WriteNuSpec(NuSpecFilePath, "Replace", nuSpecProperty: "releaseNotes", nuSpecValue: nextRelDesc.Content.ToString());

      return true;
    }

    #endregion




    #region Methods

    /// <summary>Compose the release note tag <paramref name="tag" /></summary>
    /// <param name="tag">The tag to compose a release note for</param>
    /// <param name="fromCommitExcluded">The last commit to include in the release note</param>
    /// <returns>The version description to be included in the ChangeLog</returns>
    private VersionDescription ComposeExistingVersionDescription(GitTag tag, string fromCommitExcluded)
    {
      var verDesc = new VersionDescription(tag);
      var content = this.ConcatCommitInfo(fromCommitExcluded, tag.CommitHash, Format, RefSpec);

      verDesc.Content.Append(content);

      return verDesc;
    }

    private void WriteChangeLog(IEnumerable<VersionDescription> allVersions)
    {
      StringBuilder changeLogBuilder = new StringBuilder();

      // File header
      changeLogBuilder.AppendLine("# THIS IS AN AUTOGENERATED FILE. DO NOT EDIT UNLESS YOU KNOW WHAT YOU ARE DOING.");
      changeLogBuilder.AppendLine("# CHANGE LOG\n");

      // Write all versions
      foreach (var verDesc in allVersions.OrderByDescending(v => v.No))
      {
        var header = verDesc.IsNextRelease
          ? $"[{verDesc.Name} ({verDesc.CommitHash})]"
          : $"[{verDesc.Name}]";

        // Version header
        changeLogBuilder.AppendLine(header);

        // Body
        var rawContent = verDesc.Content.ToString();

        verDesc.Content.Clear();
        ApplyChangeLogStyle(verDesc.Content, rawContent);

        changeLogBuilder.Append(verDesc.Content);

        // 2-lines break
        changeLogBuilder.AppendLine("\n");
      }

      File.WriteAllText(ChangeLogFilePath, changeLogBuilder.ToString());
    }

    /********************************************************************************************/
    /* ChangeLog example                                                                        */
    /********************************************************************************************/
    /* # THIS IS AN AUTOGENERATED FILE etc.                                                     */
    /* # CHANGE LOG : SuperMemo Assistant https://github.com/supermemo/SuperMemoAssistant/      */
    /*                                                                                          */
    /* [Next version (80ef5230d6ff7c25aa0668b60bfd8d960f064aba)]                                */
    /* - Added Notifications #53                                                                */
    /* - Added Notification + Option to restart on plugin crash                                 */
    /* - Added plugin labels #133                                                               */
    /* - Added ToolTips to UI icons #130                                                        */
    /*                                                                                          */
    /*                                                                                          */
    /* [2.0.3.309]                                                                              */
    /* - Added install destination folder in confirmation popup message #129 (Squirrel.Windows) */
    /* - Added cancel button + reset in Collection Selection #117                               */
    /* - Added a setup step to import collections from supermemo.ini #73                        */
    /*                                                                                          */
    /********************************************************************************************/
    /// <summary>
    ///   Reads [Next version (...)]'s commit hash and content, then construct a list of
    ///   revisions and their content
    /// </summary>
    /// <param name="stream">The ChangeLog stream</param>
    /// <param name="nextVersionDesc">[Next version]'s description</param>
    private Dictionary<string, VersionDescription> ReadChangeLog(Stream                 stream,
                                                                 out VersionDescription nextVersionDesc)
    {
      nextVersionDesc = null;

      VersionDescription currentVersion      = null;
      var                versionDescriptions = new List<VersionDescription>();
      var                lineNo              = 0;

      using (var sr = new StreamReader(stream))
      {
        string line;

        while ((line = sr.ReadLine()) != null)
        {
          lineNo++;

          // Skip empty lines and comments (# xxx)
          if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
            continue;

          var versionHeaderMatch = RE_ChangeLogVersionHeader.Match(line);

          if (versionHeaderMatch.Success)
          {
            var version = versionHeaderMatch.Groups["Version"].Value;
            var commit  = versionHeaderMatch.Groups["Commit"].Value;

            switch (version.ToLower())
            {
              case "next version":
                if (string.IsNullOrWhiteSpace(commit))
                  throw new ToolsException($"The [Next version] tag doesn't have a valid commit sha-1 hash: '{line}'", true);

                currentVersion = nextVersionDesc = new VersionDescription(VersionDescription.NextVersionName, commit);

                continue;

              default:
                currentVersion = new VersionDescription(version, commit);
                versionDescriptions.Add(currentVersion);

                continue;
            }
          }

          if (currentVersion == null)
            throw new ToolsException(
              $"The change log file cannot contain non-empty, non-comment lines outside of a [Version] section, line {lineNo}: '{line}'",
              true);

          currentVersion.Content.AppendLine(line);
        }
      }

      return versionDescriptions.ToDictionary(k => k.Name);
    }

    /// <summary>Enforce style on the ChangeLog</summary>
    /// <param name="outContent">Where the styled content should be written</param>
    /// <param name="content">The original content to stylized</param>
    private void ApplyChangeLogStyle(StringBuilder outContent, string content)
    {
      var hashCodes        = new HashSet<int>();
      var categories       = Categories?.Split(';') ?? new string[0];
      var categoryLinesMap = categories.ToDictionary(k => k, _ => new List<string>());
      var categorySet      = new HashSet<string>(categoryLinesMap.Keys);

      categoryLinesMap[NoCategoryKey] = new List<string>();

      // First pass over all the lines
      foreach (var line in content.SplitLines())
      {
        // Eliminate empty lines
        if (string.IsNullOrWhiteSpace(line))
          continue;

        // Fix style
        var fixedLine = ApplyLineStyle(line, categorySet, out var category);

        // Eliminate copies
        var hashCode = line.ToLower(CultureInfo.InvariantCulture).GetHashCode();

        if (hashCodes.Contains(hashCode))
          continue;

        hashCodes.Add(hashCode);

        // Group by category
        categoryLinesMap[category].Add(fixedLine);
      }

      // Write the ordered content in the same order as the provided Categories
      foreach (var category in categories)
      foreach (var line in categoryLinesMap[category])
        outContent.AppendLine(line);

      // Write the content that do not belong to any category last
      foreach (var line in categoryLinesMap[NoCategoryKey])
        outContent.AppendLine(line);
    }

    /// <summary>Make sure the line has a "- xxx" bullet, accounting for indentation</summary>
    /// <param name="line"></param>
    /// <param name="categories"></param>
    /// <param name="category"></param>
    /// <returns></returns>
    private string ApplyLineStyle(string line, HashSet<string> categories, out string category)
    {
      if (line.TrimStart().StartsWith("-") == false)
      {
        var indentMatch = RE_Indent.Match(line);
        var indentation = indentMatch.Success
          ? indentMatch.Groups[0].Value
          : string.Empty;

        line = indentation + "- " + line;
      }

      category = RE_LineCategory.Match(line).Groups["Category"].Value;

      if (categories.Contains(category) == false)
        category = NoCategoryKey;

      return line;
    }

    #endregion




    private class VersionDescription : GitTag
    {
      #region Constants & Statics

      public const string NextVersionName = "Next version";

      #endregion




      #region Constructors

      public VersionDescription(GitTag gitTag)
        : base(gitTag.No, gitTag.Name, gitTag.CommitHash) { }

      public VersionDescription(string version, string commitHash, int no = -1)
        : base(no, version, commitHash) { }

      #endregion




      #region Properties & Fields - Public

      public StringBuilder Content { get; } = new StringBuilder();

      public bool IsNextRelease => Name == NextVersionName;

      #endregion
    }
  }
}
