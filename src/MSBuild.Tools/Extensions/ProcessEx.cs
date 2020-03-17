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
// Modified On:  2020/03/14 15:16
// Modified By:  Alexis

#endregion




using System.Diagnostics;
using System.Text;

namespace MSBuild.Tools.Extensions
{
  public static class ProcessEx
  {
    #region Methods

    public static Process CreateBackgroundProcess(string binName,
                                                  string args,
                                                  string workingDirectory = null)
    {
      Process p = new Process
      {
        StartInfo =
        {
          FileName        = binName,
          Arguments       = args,
          UseShellExecute = false,
          CreateNoWindow  = true,
        }
      };

      if (workingDirectory != null)
        p.StartInfo.WorkingDirectory = workingDirectory;

      return p;
    }

    public static int ExecuteBlockingWithOutputs(
      this Process p,
      out  string  output,
      out  string  stdOutput,
      out  string  errOutput,
      out  bool    timedOut,
      int          timeout = int.MaxValue,
      bool         kill    = true)
    {
      timedOut = false;

      StringBuilder outputBuilder    = new StringBuilder();
      StringBuilder stdOutputBuilder = new StringBuilder();
      StringBuilder errOutputBuilder = new StringBuilder();

      void OutputDataReceived(DataReceivedEventArgs e,
                              StringBuilder         builder)
      {
        lock (builder)
        {
          builder.AppendLine(e.Data);
          outputBuilder.AppendLine(e.Data);
        }
      }

      p.EnableRaisingEvents              =  true;
      p.StartInfo.RedirectStandardOutput =  true;
      p.StartInfo.RedirectStandardError  =  true;
      p.OutputDataReceived               += (_, e) => OutputDataReceived(e, stdOutputBuilder);
      p.ErrorDataReceived                += (_, e) => OutputDataReceived(e, errOutputBuilder);

      try
      {
        p.Start();

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        if (!p.WaitForExit(timeout))
        {
          if (kill)
            try
            {
              p.Kill();
            }
            catch
            {
              // ignored
            }

          timedOut = true;

          return -1;
        }

        p.WaitForExit();

        return p.ExitCode;
      }
      finally
      {
        output    = outputBuilder.ToString();
        stdOutput = stdOutputBuilder.ToString();
        errOutput = errOutputBuilder.ToString();

        p.Dispose();
      }
    }

    #endregion
  }
}
