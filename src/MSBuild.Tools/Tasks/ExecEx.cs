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




using Microsoft.Build.Framework;
using MSBuild.Tools.Extensions;
using MSBuild.Tools.Helpers;

namespace MSBuild.Tools.Tasks
{
  public class ExecEx : TaskBase
  {
    #region Properties & Fields - Public

    /// <summary>The process executable name</summary>
    [Required]
    public string Executable { get; set; }

    /// <summary>Optional arguments to pass to the executable</summary>
    public string Arguments { get; set; }

    /// <summary>Executable working directory for the process execution</summary>
    public string WorkingDirectory { get; set; }

    /// <summary>
    ///   Whether to fail the task when the process doesn't return a 0 exit code, default to
    ///   false
    /// </summary>
    public bool FailOnNonZeroExitCode { get; set; }

    /// <summary>Optional time out for the process' execution</summary>
    public int ProcessTimeOut { get; set; } = int.MaxValue;

    /// <summary>Process' Standard + Error outputs combined</summary>
    [Output]
    public string AllOutput { get; private set; }

    /// <summary>Process' Standard output</summary>
    [Output]
    public string StandardOutput { get; private set; }

    /// <summary>Process' Error output</summary>
    [Output]
    public string ErrorOutput { get; private set; }

    /// <summary>Process' Exit code</summary>
    [Output]
    public int ExitCode { get; private set; }

    /// <summary>Whether the process timed out (if <see cref="ProcessTimeOut" /> is defined)</summary>
    [Output]
    public bool TimedOut { get; private set; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool ExecuteTask()
    {
      var proc = ProcessEx.CreateBackgroundProcess(Executable, Arguments, WorkingDirectory);

      ExitCode = proc.ExecuteBlockingWithOutputs(out var output, out var stdOutput, out var errOutput, out var timedOut, ProcessTimeOut);

      AllOutput      = output;
      StandardOutput = stdOutput;
      ErrorOutput    = errOutput;
      TimedOut       = timedOut;

      return !FailOnNonZeroExitCode || ExitCode == 0;
    }

    #endregion
  }
}
