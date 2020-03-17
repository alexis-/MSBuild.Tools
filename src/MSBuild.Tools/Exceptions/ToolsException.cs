﻿#region License & Metadata

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
// Modified On:  2020/03/14 23:18
// Modified By:  Alexis

#endregion




using System;

namespace MSBuild.Tools.Exceptions
{
  /// <summary>
  ///   Used by tasks under the <see cref="Tasks.Git" /> to catch git-related errors and
  ///   control the program flow
  /// </summary>
  internal class ToolsException : Exception
  {
    #region Constructors

    /// <inheritdoc />
    public ToolsException(string message, bool isFatalException) : base(message)
    {
      IsFatalException = isFatalException;
    }

    /// <inheritdoc />
    public ToolsException(string message, bool isFatalException, Exception innerException) : base(message, innerException)
    {
      IsFatalException = isFatalException;
    }

    #endregion




    #region Properties & Fields - Public

    public bool IsFatalException { get; }

    #endregion
  }
}
