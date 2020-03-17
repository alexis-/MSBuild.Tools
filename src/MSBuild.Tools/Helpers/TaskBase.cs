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
// Modified On:  2020/03/16 02:49
// Modified By:  Alexis

#endregion




using System;
using System.Diagnostics;
using System.Resources;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using MSBuild.Tools.Exceptions;

namespace MSBuild.Tools.Helpers
{
  public abstract class TaskBase : Task
  {
    #region Constructors

    /// <inheritdoc />
    protected TaskBase() { }

    /// <inheritdoc />
    protected TaskBase(ResourceManager taskResources) : base(taskResources) { }

    /// <inheritdoc />
    protected TaskBase(ResourceManager taskResources, string helpKeywordPrefix) : base(taskResources, helpKeywordPrefix) { }

    #endregion




    #region Properties & Fields - Public

    /// <summary>Whether to attach the debugger</summary>
    public bool Debug { get; set; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public sealed override bool Execute()
    {
      try
      {
        if (Debug)
          Debugger.Launch();

        return ExecuteTask();
      }
      catch (ToolsException gitEx)
      {
        if (gitEx.IsFatalException)
          LogError(gitEx.Message);

        else
          LogInfo(gitEx.Message);

        return gitEx.IsFatalException;
      }
      catch (Exception ex)
      {
        LogError(ex.ToString());

        return false;
      }
    }

    #endregion




    #region Methods

    public virtual void LogMessage(string message, int level)
    {
      BuildEngine.LogMessageEvent(new BuildMessageEventArgs(PrependMessage(message), "", "MSBuild.Tools", (MessageImportance)level));
    }

    public virtual void LogDebug(string message)
    {
      BuildEngine.LogMessageEvent(new BuildMessageEventArgs(PrependMessage(message), "", "MSBuild.Tools", MessageImportance.Low));
    }

    public virtual void LogInfo(string message)
    {
      BuildEngine.LogMessageEvent(new BuildMessageEventArgs(PrependMessage(message), "", "MSBuild.Tools", MessageImportance.Normal));
    }

    public virtual void LogWarning(string message, string code)
    {
      LogWarning(message, null, 0, 0, 0, 0, code);
    }

    public virtual void LogWarning(string message,
                                   string file,
                                   int    lineNumber,
                                   int    columnNumber,
                                   int    endLineNumber,
                                   int    endColumnNumber,
                                   string code)
    {
      BuildEngine.LogWarningEvent(new BuildWarningEventArgs("", code ?? "", file, lineNumber, columnNumber, endLineNumber, endColumnNumber,
                                                            PrependMessage(message), "", "MSBuild.Tools"));
    }

    public virtual void LogError(string message)
    {
      LogError(message, null, 0, 0, 0, 0);
    }

    public virtual void LogError(string message, string file, int lineNumber, int columnNumber, int endLineNumber, int endColumnNumber)
    {
      BuildEngine.LogErrorEvent(new BuildErrorEventArgs("", "", file, lineNumber, columnNumber, endLineNumber, endColumnNumber,
                                                        PrependMessage(message), "", "Fody"));
    }

    private string PrependMessage(string message)
    {
      return $"MSBuild.Tools/{GetType().Name}: {message}";
    }

    #endregion




    #region Methods Abs

    /// <summary>When overridden in a derived class, executes the task.</summary>
    /// <returns>
    ///   <see langword="true" /> if the task successfully executed; otherwise,
    ///   <see langword="false" />.
    /// </returns>
    public abstract bool ExecuteTask();

    #endregion
  }
}
