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
// Modified On:  2020/03/30 14:53
// Modified By:  Alexis

#endregion




using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MSBuild.Tools.Extensions
{
  public static class JsonEx
  {
    #region Methods

    public static string Serialize(this object obj,
                                   Formatting  format = Formatting.None)
    {
      return JsonConvert.SerializeObject(obj, format);
    }

    public static T Deserialize<T>(this string json)
    {
      return JsonConvert.DeserializeObject<T>(json);
    }

    public static JObject Deserialize(this string json)
    {
      return JObject.Parse(json);
    }

    public static void WriteToFile(this JObject obj,
                                   string       filePath,
                                   Formatting   format = Formatting.None)
    {
      using var fs = File.Open(filePath, FileMode.Create, FileAccess.Write);
      using var sw = new StreamWriter(fs);
      using var jw = new JsonTextWriter(sw) { Formatting = format };

      obj.WriteTo(jw);
    }

    public static void SerializeToFile(this object obj,
                                       string      filePath,
                                       Formatting  format = Formatting.None)
    {
      using var fs     = File.Open(filePath, FileMode.Create, FileAccess.Write);
      using var writer = new StreamWriter(fs);

      writer.Write(obj.Serialize(format));
    }

    public static T DeserializeFromFile<T>(this string filePath)
    {
      using var fs     = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      using var reader = new StreamReader(fs);

      return Deserialize<T>(reader.ReadToEnd());
    }

    public static JObject DeserializeFromFile(this string filePath)
    {
      using var fs     = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
      using var reader = new StreamReader(fs);

      return Deserialize(reader.ReadToEnd());
    }

    #endregion
  }
}
