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
// Modified On:  2020/02/24 20:08
// Modified By:  Alexis

#endregion




using System;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using OpenMcdf;

namespace MSBuild.Tools
{
  // Original https://stackoverflow.com/questions/55478699/is-there-any-way-to-get-the-active-solution-configuration-name-in-c-sharp-code
  public class ReadSuoTask : Task, ITask
  {
    #region Constants & Statics

    private const string SolutionConfigStreamName = "SolutionConfiguration";
    private const string ActiveConfigTokenName    = "ActiveCfg";

    #endregion




    #region Properties & Fields - Public

    [Required]
    public string SuoFilePath { get; set; }

    [Output]
    public string SolutionConfiguration { get; private set; }
    [Output]
    public string SolutionPlatform { get; private set; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool Execute()
    {
      try
      {
        var activeCfg      = ExtractActiveSolutionConfig(new FileInfo(SuoFilePath));
        var activeCfgSplit = activeCfg.Split('|');

        if (activeCfgSplit.Length != 2)
          return false;

        SolutionConfiguration = activeCfgSplit[0];
        SolutionPlatform      = activeCfgSplit[1];

        return true;
      }
      catch (ProgramResultException e)
      {
        Log.LogError(e.Message);

        return false;
      }
    }

    #endregion




    #region Methods

    private static string ExtractActiveSolutionConfig(FileInfo fromSuoFile)
    {
      CompoundFile compoundFile;

      try
      {
        compoundFile = new CompoundFile(fromSuoFile.FullName);
      }
      catch (CFFileFormatException)
      {
        throw CreateInvalidFileFormatProgramResultException(fromSuoFile);
      }

      if (compoundFile.RootStorage.TryGetStream(
        SolutionConfigStreamName, out CFStream compoundFileStream))
      {
        var    data                   = compoundFileStream.GetData();
        string dataAsString           = Encoding.GetEncoding("UTF-16").GetString(data);
        int    activeConfigTokenIndex = dataAsString.LastIndexOf(ActiveConfigTokenName, StringComparison.Ordinal);

        if (activeConfigTokenIndex < 0)
          CreateInvalidFileFormatProgramResultException(fromSuoFile);

        string afterActiveConfigToken =
          dataAsString.Substring(activeConfigTokenIndex);

        int    lastNullCharIdx = afterActiveConfigToken.LastIndexOf('\0');
        string ret             = afterActiveConfigToken.Substring(lastNullCharIdx + 1);
        return ret.Replace(";", "");
      }
      else
      {
        throw CreateInvalidFileFormatProgramResultException(fromSuoFile);
      }
    }

    private static ProgramResultException CreateInvalidFileFormatProgramResultException(
      FileInfo invalidFile) => new ProgramResultException(
      $@"The provided file ""{invalidFile.FullName}"" is not a valid " +
      $@"SUO file with a ""{SolutionConfigStreamName}"" stream and an " +
      $@"""{ActiveConfigTokenName}"" token.");

    #endregion
  }

  internal class ProgramResultException : Exception
  {
    #region Constructors

    internal ProgramResultException(string message)
      : base(message) { }

    #endregion
  }
}
