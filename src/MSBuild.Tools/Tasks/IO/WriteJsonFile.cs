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
// Modified On:  2020/03/30 14:56
// Modified By:  Alexis

#endregion




using System;
using System.Collections;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using MSBuild.Tools.Extensions;
using MSBuild.Tools.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ObjDict = System.Collections.Generic.Dictionary<string, object>;

namespace MSBuild.Tools.Tasks.IO
{
  /// <summary>
  ///   Writes NodesContent in json format to file using the provided optional node paths to
  ///   preserve the json object hierarchy
  /// </summary>
  public class WriteJsonFile : TaskBase
  {
    #region Properties & Fields - Public

    /// <summary>The file path to write</summary>
    [Required]
    public string FilePath { get; set; }

    /// <summary>The content to write</summary>
    [Required]
    public ITaskItem[] NodesContent { get; set; }

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    public override bool ExecuteTask()
    {
      if (NodesContent.Any() == false)
        return true;

      // File doesn't exists, create content from scratch
      if (File.Exists(FilePath) == false)
      {
        var root = new ObjDict();

        foreach (var nodeContent in NodesContent)
        {
          var props = TaskItemToNodeDictionary(nodeContent, out var nodeNames);
          root = BuildHierarchy(root, nodeNames, props);
        }

        root.SerializeToFile(FilePath, Formatting.Indented);

        return true;
      }

      // Read json
      var jsonObj = FilePath.DeserializeFromFile();

      // Update JObject
      foreach (var nodeContent in NodesContent)
      {
        var props = TaskItemToNodeDictionary(nodeContent, out var nodeNames);
        BuildHierarchy(jsonObj, nodeNames, props);
      }

      // Commit changes
      jsonObj.WriteToFile(FilePath, Formatting.Indented);

      return true;
    }

    #endregion




    #region Methods

    /// <summary>
    ///   Builds the <paramref name="nodeNames" /> hierarchy into <paramref name="root" /> and
    ///   set the last node level to <paramref name="props" />
    /// </summary>
    /// <param name="root"></param>
    /// <param name="nodeNames"></param>
    /// <param name="props"></param>
    /// <returns></returns>
    private void BuildHierarchy(
      JObject  root,
      string[] nodeNames,
      ObjDict  props)
    {
      if (nodeNames == null || nodeNames.Any() == false)
      {
        MergeJObject(root, props);
        return;
      }

      string nodeName;
      var    i  = 0;
      var    it = root;

      for (; i < nodeNames.Length - 1; i++)
      {
        nodeName = nodeNames[i];

        if (it.ContainsKey(nodeName))
        {
          it[nodeName] = it = GetJObjectOrNew(
            it, nodeName,
            $"Erasing existing property {nodeName} for a new node");
          continue;
        }

        var newNode = new JObject();

        it[nodeName] = it = newNode;
      }

      nodeName = nodeNames[i];

      var lastNode = GetJObjectOrNew(it, nodeName);
      MergeJObject(lastNode, props);
    }

    /// <summary>
    ///   Builds the <paramref name="nodeNames" /> hierarchy into <paramref name="root" /> and
    ///   set the last node level to <paramref name="props" />
    /// </summary>
    /// <param name="root"></param>
    /// <param name="nodeNames"></param>
    /// <param name="props"></param>
    /// <returns></returns>
    private ObjDict BuildHierarchy(
      ObjDict  root,
      string[] nodeNames,
      ObjDict  props)
    {
      if (nodeNames == null || nodeNames.Any() == false)
        return MergeDictionaries(root, props);

      string nodeName;
      var    i  = 0;
      var    it = root;

      for (; i < nodeNames.Length - 1; i++)
      {
        nodeName = nodeNames[i];

        if (it.ContainsKey(nodeName))
        {
          it[nodeName] = it = GetObjDictOrNew(
            it, nodeName,
            $"Erasing existing property {nodeName} for a new node");
          continue;
        }

        var newNode = new ObjDict();

        it[nodeName] = it = newNode;
      }

      nodeName = nodeNames[i];

      var lastNode = GetObjDictOrNew(it, nodeName);
      it[nodeName] = MergeDictionaries(lastNode, props);

      return root;
    }

    /// <summary>
    ///   Lookups <paramref name="root" /> for a key named <paramref name="propName" /> and
    ///   casts it to <see cref="JObject" /> if it exists. If the cast fails returns a new
    ///   <see cref="JObject" /> instead, possibly with a warning. If the key doesn't exist, returns a
    ///   new <see cref="JObject" />.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="propName"></param>
    /// <param name="warningMsg"></param>
    /// <returns></returns>
    private JObject GetJObjectOrNew(
      JObject root,
      string  propName,
      string  warningMsg = null)
    {
      if (root == null || root.ContainsKey(propName) == false)
        return new JObject();

      if (root[propName] is JObject newNode)
        return newNode;

      if (warningMsg != null)
        LogWarning(warningMsg);

      return new JObject();
    }

    /// <summary>
    ///   Lookups <paramref name="root" /> for a key named <paramref name="propName" /> and
    ///   casts it to <see cref="ObjDict" /> if it exists. If the cast fails returns a new
    ///   <see cref="ObjDict" /> instead, possibly with a warning. If the key doesn't exist, returns a
    ///   new <see cref="ObjDict" />.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="propName"></param>
    /// <param name="warningMsg"></param>
    /// <returns></returns>
    private ObjDict GetObjDictOrNew(ObjDict root, string propName, string warningMsg = null)
    {
      if (root == null || root.ContainsKey(propName) == false)
        return new ObjDict();

      if (root[propName] is ObjDict dict)
        return dict;

      if (warningMsg != null)
        LogWarning(warningMsg);

      return new ObjDict();
    }

    /// <summary>
    ///   Merges <paramref name="props" /> into <paramref name="root" />. Values from
    ///   <paramref name="props" /> are selected for duplicate keys.
    /// </summary>
    /// <param name="root"></param>
    /// <param name="props"></param>
    private void MergeJObject(JObject root, ObjDict props)
    {
      foreach (var prop in props)
        if (root.ContainsKey(prop.Key))
          root[prop.Key] = JToken.FromObject(prop.Value);

        else
          root.Add(prop.Key, JToken.FromObject(prop.Value));
    }

    /// <summary>
    ///   Merges two dictionary. Values from <paramref name="dict2" /> are selected for
    ///   duplicate keys
    /// </summary>
    /// <param name="dict1"></param>
    /// <param name="dict2"></param>
    /// <returns></returns>
    private ObjDict MergeDictionaries(ObjDict dict1, ObjDict dict2)
    {
      return dict2.Concat(dict1)
                  .GroupBy(e => e.Key)
                  .ToDictionary(g => g.Key, g => g.First().Value);
    }

    /// <summary>
    ///   Parses the <paramref name="taskItem" /> and transforms it to a <see cref="ObjDict" />
    ///   . Handles string, int and double values only.
    /// </summary>
    /// <param name="taskItem"></param>
    /// <param name="nodeNames"></param>
    /// <returns></returns>
    private ObjDict TaskItemToNodeDictionary(ITaskItem taskItem, out string[] nodeNames)
    {
      var props = new ObjDict();

      nodeNames = taskItem.ItemSpec == null || string.IsNullOrWhiteSpace(taskItem.ItemSpec)
        ? null
        : taskItem.ItemSpec
                  .Split('.')
                  .Where(s => string.IsNullOrEmpty(s) == false)
                  .ToArray();

      foreach (DictionaryEntry propKeyValue in taskItem.CloneCustomMetadata())
      {
        var propName = (string)propKeyValue.Key;
        var propValue = (string)propKeyValue.Value;

        if (propName.StartsWith("\"") && propName.EndsWith("\""))
          props[propName] = propValue;

        else if (int.TryParse(propValue, out int propIntValue))
          props[propName] = propIntValue;

        else if (double.TryParse(propValue, out double propDoubleValue))
          props[propName] = propDoubleValue;

        else
          props[propName] = propValue;
      }

      return props;
    }

    #endregion
  }
}
