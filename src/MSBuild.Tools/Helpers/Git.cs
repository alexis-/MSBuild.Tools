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
// Modified On:  2020/03/17 15:51
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MSBuild.Tools.Exceptions;
using MSBuild.Tools.Extensions;
using MSBuild.Tools.Models;
using MSBuild.Tools.Tasks.Git;

namespace MSBuild.Tools.Helpers
{
  public static class Git
  {
    #region Constants & Statics

    private static readonly Regex RE_Variable = new Regex("!\\((?<cmd>[\\w-]+)(?:\\:(?<hash>[\\w]{40}))?(?<lookAhead>(\\+|\\-)[\\d]+)?\\)",
                                                          RegexOptions.Compiled);

    private static readonly Regex RE_Refs =
      new Regex("^((?:(?:(?:refs/)?(?:heads|remotes(?:/[\\w-\\.]+)?|tags)/)[\\w-\\.]+|(?:[\\w-\\.]+/)?HEAD))$");

    #endregion




    #region Methods

    public static string ConcatCommitInfo(
      this GitTaskBase taskBase,
      string           fromCommitExcluded,
      string           toCommitIncluded,
      string           format                 = "format:%B",
      string           refSpec                = "refs/remotes/origin/HEAD",
      bool             noMerges               = true,
      bool             exFatal                = true,
      bool             throwOnNonZeroExitCode = true)
    {
      if (string.IsNullOrWhiteSpace(fromCommitExcluded) == false)
        fromCommitExcluded = fromCommitExcluded + "..";

      if (string.IsNullOrWhiteSpace(toCommitIncluded))
        toCommitIncluded = refSpec;

      var noMergesStr = noMerges ? "--no-merges" : string.Empty;

      return taskBase.ExecuteGit($"log {noMergesStr} --pretty={format} {fromCommitExcluded}{toCommitIncluded}",
                                 "Failed to concat commit info",
                                 exFatal, throwOnNonZeroExitCode)
                     .SplitLines()
                     .Where(l => string.IsNullOrWhiteSpace(l) == false)
                     .Join("\n");
    }

    /// <summary>List all commits in the given remote and branch</summary>
    /// <param name="taskBase">The MSBuild task</param>
    /// <param name="refSpec">The Git refSpec</param>
    /// <param name="exFatal">Whether an error is fatal</param>
    /// <param name="throwOnNonZeroExitCode">Whether to throw if Git returns a non-zero exit code</param>
    /// <returns></returns>
    public static string GetAllCommitsHash(
      this GitTaskBase taskBase,
      string           refSpec                = "refs/remotes/origin/HEAD",
      bool             exFatal                = true,
      bool             throwOnNonZeroExitCode = true)
    {
      return taskBase.ExecuteGit($"rev-list {refSpec}",
                                 $"An error occured while listing all commits in {refSpec}",
                                 exFatal,
                                 throwOnNonZeroExitCode);
    }

    public static string GetCommitInfo(
      this GitTaskBase taskBase,
      string           hash,
      string           format = "format:%B")
    {
      return taskBase.ExecuteGit($"log -1 --pretty={format} {hash}",
                                 $"No such commit found: {hash}");
    }

    public static string GetTagCommit(
      this GitTaskBase taskBase,
      string           tag,
      bool             exFatal                = true,
      bool             throwOnNonZeroExitCode = true)
    {
      return taskBase.ExecuteGit($"rev-parse {tag}",
                                 $"No such tag found: {tag}",
                                 exFatal,
                                 throwOnNonZeroExitCode);
    }

    /// <summary>Lists all tags and their commit hash</summary>
    /// <param name="taskBase">The MSBuild task</param>
    /// <param name="refSpec">The Git refSpec</param>
    /// <param name="exFatal">Whether an error is fatal</param>
    /// <param name="throwOnNonZeroExitCode">Whether to throw if Git returns a non-zero exit code</param>
    /// <returns>Map of tag name -> tag object</returns>
    public static Dictionary<string, GitTag> GetTagCommitMap(
      this GitTaskBase taskBase,
      string           refSpec                = "refs/remotes/origin/HEAD",
      bool             exFatal                = true,
      bool             throwOnNonZeroExitCode = true)
    {
      var merged = string.IsNullOrWhiteSpace(refSpec) == false
        ? $"--merged={refSpec}"
        : string.Empty;
      var output = taskBase.ExecuteGit($"tag -l {merged} --format=\"%(refname:strip=2) %(objecttype) %(objectname) %(object)\"",
                                       "An exception occured while list tags and their commit hash",
                                       exFatal,
                                       throwOnNonZeroExitCode);

      var map = new Dictionary<string, GitTag>();
      int i   = 0;

      if (string.IsNullOrWhiteSpace(output))
        return map;

      foreach (var line in output.SplitLines(StringSplitOptions.RemoveEmptyEntries))
      {
        var splitLine = line.Split(' ');

        if (splitLine.Length != 4)
          throw new ToolsException($"GetTagCommitMap: tag commit list line is malformatted: '{line}'", true);

        var    name = splitLine[0];
        var    type = splitLine[1];
        string commitHash;

        switch (type)
        {
          case "commit":
            commitHash = splitLine[2];
            break;

          case "tag":
          default:
            commitHash = splitLine[3];
            break;
        }

        map[name] = new GitTag(i++, name, commitHash);
      }

      return map;
    }

    public static string ExpandToCommitHash(
      this GitTaskBase taskBase,
      string           refSpecOrCommitOrVariable,
      string           branch                 = "HEAD",
      string           remote                 = "origin",
      bool             noMerges               = true,
      bool             exFatal                = true,
      bool             throwOnNonZeroExitCode = true)
    {
      string ExecGit(string args, string errMsg)
      {
        return taskBase.ExecuteGit(
          args,
          $"An error occured while {errMsg}.\nCommand: '{0}'.\nOutput: '{1}'.",
          exFatal,
          throwOnNonZeroExitCode).Trim();
      }

      refSpecOrCommitOrVariable = refSpecOrCommitOrVariable.Trim();

      var refMatch = RE_Refs.Match(refSpecOrCommitOrVariable);

      if (refMatch.Success)
        return ExecGit($"rev-parse {refSpecOrCommitOrVariable}", "parsing refspec");

      var varMatch = RE_Variable.Match(refSpecOrCommitOrVariable);

      if (varMatch.Success == false)
        return refSpecOrCommitOrVariable;

      var variable  = varMatch.Groups["cmd"].Value;
      var hash      = varMatch.Groups["hash"]?.Value;
      int lookAhead = 0;

      if (varMatch.Groups["lookAhead"] != null)
        lookAhead = int.Parse(varMatch.Groups["lookAhead"].Value);

      string commit;

      switch (variable)
      {
        case "latestTag":
          commit = ExecGit("rev-list --tags --max-count=1",
                           "fetching the latest tag's commit hash").FirstLine();
          break;

        case "latestCommit":
          lookAhead = Math.Max(lookAhead, 0);

          return ExecGit($"rev-list --max-count={lookAhead + 1}",
                         "fetching the latest commit hash").LastLine();

        case "commit":
          if (string.IsNullOrWhiteSpace(hash))
            throw new ToolsException("You must provide a commit hash, e.g. !(commit:48e217628ba110041c923420933cb548c28ec58d)", true);

          commit = hash;
          break;

        default:
          throw new ToolsException($"ExpandToCommitHash: Unknown commit variable: {refSpecOrCommitOrVariable}", true);
      }

      var noMergesStr = noMerges ? "--no-merges" : string.Empty;

      if (lookAhead > 0)
      {
        var commits = ExecGit($"rev-list {noMergesStr} --date-order --reverse {commit}..{remote}/{branch}",
                              $"looking forward {lookAhead} commits from {commit}");

        var commitsLines = commits.SplitLines();

        commit = commitsLines[Math.Min(lookAhead - 1, commitsLines.Length - 1)].DefaultIfWhiteSpaceOrNull(commit);
      }

      else if (lookAhead < 0)
      {
        lookAhead = -lookAhead;
        var commits = ExecGit($"rev-list {noMergesStr} --date-order --max-count={lookAhead + 1} {commit}",
                              $"looking backward {lookAhead} commits from {commit}");

        commit = commits.LastLine().DefaultIfWhiteSpaceOrNull(commit);
      }

      return commit;
    }

    #endregion
  }
}
