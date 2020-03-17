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
// Modified On:  2020/03/15 12:59
// Modified By:  Alexis

#endregion




using Microsoft.Build.Framework;
using MSBuild.Tools.Exceptions;
using MSBuild.Tools.Extensions;
using MSBuild.Tools.Helpers;

namespace MSBuild.Tools.Tasks.Git
{
  public abstract class GitTaskBase : TaskBase
  {
    #region Properties & Fields - Public

    /// <summary>The git executable name or file path</summary>
    [Required]
    public string GitExecutable { get; set; }

    /// <summary>Optional working directory for the process' execution</summary>
    public string WorkingDirectory { get; set; }

    /// <summary>Optional time out for the process' execution</summary>
    public int ProcessTimeOut { get; set; } = int.MaxValue;

    /// <summary>Optional debug flag for git tasks. Prints git commands and their output</summary>
    public bool GitDebug { get; set; }

    /// <summary>Whether the process timed out (if <see cref="ProcessTimeOut" /> is defined)</summary>
    [Output]
    public bool TimedOut { get; private set; }

    #endregion




    #region Methods

    public string ExecuteGit(string args,
                             string exMsg                  = "An error occured while executing git command '{0}'.\nOutput: {1}",
                             bool   exFatal                = true,
                             bool   throwOnNonZeroExitCode = true)
    {
      int    exitCode  = -2;
      string allOutput = string.Empty;
      bool   timedOut  = false;

      try
      {
        var gitFetch = ProcessEx.CreateBackgroundProcess(GitExecutable, args, WorkingDirectory);
        exitCode = gitFetch.ExecuteBlockingWithOutputs(
          out allOutput,
          out string stdOutput,
          out _,
          out timedOut,
          ProcessTimeOut);

        TimedOut = timedOut;

        if (throwOnNonZeroExitCode && exitCode != 0)
        {
          var gitCmd = $"{GitExecutable} \"{args}\"";

          throw new ToolsException(string.Format(exMsg, gitCmd, allOutput), exFatal);
        }

        return stdOutput;
      }
      finally
      {
        if (GitDebug)
          LogInfo($"Ran command: '{GitExecutable} {args}'. Timed out ? {timedOut}. Exit code {exitCode}. Output:\n{allOutput}");
      }
    }

    #endregion
  }
}
