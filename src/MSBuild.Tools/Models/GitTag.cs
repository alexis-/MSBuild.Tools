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
// Modified On:  2020/03/16 00:53
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;

namespace MSBuild.Tools.Models
{
  /// <summary>Represents a Git tag</summary>
  public class GitTag
    : IEquatable<GitTag>
  {
    #region Constants & Statics

    public static IComparer<GitTag> Comparer { get; } = new GitTagComparer();

    #endregion




    #region Constructors

    public GitTag(int no, string name, string commitHash)
    {
      No         = no;
      Name       = name;
      CommitHash = commitHash;
    }

    #endregion




    #region Properties & Fields - Public

    /// <summary>Identifies the tag's placement in the tags timeline</summary>
    public int No { get; set; }

    /// <summary>The tag's name (e.g. 2.0.3.309)</summary>
    public string Name { get; }

    /// <summary>The commit hash associated with this tag</summary>
    public string CommitHash { get; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
        return false;
      if (ReferenceEquals(this, obj))
        return true;
      if (obj.GetType() != GetType())
        return false;

      return Equals((GitTag)obj);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
      unchecked
      {
        return ((Name != null ? Name.GetHashCode() : 0) * 397) ^ (CommitHash != null ? CommitHash.GetHashCode() : 0);
      }
    }

    /// <inheritdoc />
    public bool Equals(GitTag other)
    {
      if (ReferenceEquals(null, other))
        return false;
      if (ReferenceEquals(this, other))
        return true;

      return Name == other.Name && CommitHash == other.CommitHash;
    }

    #endregion




    #region Methods

    public static bool operator ==(GitTag left, GitTag right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(GitTag left, GitTag right)
    {
      return !Equals(left, right);
    }

    #endregion
  }


  /// <summary>GitTag comparer</summary>
  public class GitTagComparer : IComparer<GitTag>
  {
    #region Methods Impl

    /// <inheritdoc />
    public int Compare(GitTag x, GitTag y)
    {
      return x.No.CompareTo(y);
    }

    #endregion
  }
}
