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
// Modified On:  2020/03/16 02:18
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;

namespace MSBuild.Tools.Extensions
{
  /// <summary>Extensions for <see cref="string" /></summary>
  public static class StringEx
  {
    #region Methods

    public static string[] SplitLines(this string str, StringSplitOptions options = StringSplitOptions.None)
    {
      return str.Split(new[] { "\r\n", "\n", "\r" }, options);
    }

    public static string DefaultIfWhiteSpaceOrNull(this string str, string defaultValue)
    {
      if (string.IsNullOrWhiteSpace(str))
        return defaultValue;

      return str;
    }

    public static string Join<T>(this IEnumerable<T> enums, string separator)
    {
      return string.Join(separator, enums);
    }

    public static string FirstLine(this string lines)
    {
      int firstCarryReturn = lines.IndexOf('\r');
      int firstLineFeed    = lines.IndexOf('\n');
      int cutAt            = Math.Min(firstCarryReturn, firstLineFeed);

      if (cutAt == 0)
        return string.Empty;

      if (cutAt < 0)
        return lines;

      return lines.Substring(0, cutAt);
    }

    public static string LastLine(this string lines)
    {
      int firstCarryReturn = lines.LastIndexOf('\r');
      int firstLineFeed    = lines.LastIndexOf('\n');
      int from             = Math.Max(firstCarryReturn, firstLineFeed);

      if (from + 1 == lines.Length)
        return string.Empty;

      if (from < 0)
        return lines;

      return lines.Substring(from + 1);
    }

    #endregion
  }
}
