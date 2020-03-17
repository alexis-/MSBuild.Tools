using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using MSBuild.Tools.Models;

namespace MSBuild.Tools.Helpers
{
  public static class NuGet
  {
    #region Constants & Statics

    private const string NuSpecPropValuesUsage = @"NuSpecPropValues Usage:

<ItemGroup>
  <MyNuSpecPropValues>
    <Property>id</Property>
    <Value>MSBuild.Tools</Value>
  </MyNuSpecPropValues>
  <MyNuSpecPropValues>
    <Property>releaseNotes</Property>
    <Value>- Added WriteNuSpec task</Value>
    <EditMode>Append</EditMode>
  </MyNuSpecPropValues>
</ItemGroup>

<WriteNuSpec NuSpecPropValues=""@(MyNuSpecPropValues)"" />";

    private const string NuSpecSinglePropValueUsage = @"Usage:

<PropertyGroup>
  <MyNuSpecProperty>Title</MyNuSpecProperty>
  <MyNuSpecValue>Tools for MSBuild</MyNuSpecValue>
</PropertyGroup>

<WriteNuSpec NuSpecProperty=""$(MyNuSpecProperty)"" NuSpecValue=""$(MyNuSpecValue)"" />";

    #endregion

    public static bool WriteNuSpec(
      this TaskBase task,
      string      nuSpecFilePath,
      string      editMode         = "Replace",
      ITaskItem[] nuSpecPropValues = null,
      string      nuSpecProperty   = null,
      string      nuSpecValue      = null,
      string      nuSpecSection    = "metadata",
      bool        failIfEmpty      = true)
    {
      Dictionary<string, PropertyEdit> propValues = new Dictionary<string, PropertyEdit>();

      if (nuSpecPropValues != null && (nuSpecProperty != null || nuSpecValue != null))
      {
        task.LogError(
          "Invalid parameters. Parameter NuSpecPropValues is mutually exclusive with (NuSpecProperty and NuSpecValue). Only use one of the two solutions.");
        return false;
      }

      if (nuSpecPropValues != null)
      {
        if (nuSpecPropValues.Any(nspv => string.IsNullOrWhiteSpace(nspv.GetMetadata("Property"))
                                   || string.IsNullOrWhiteSpace(nspv.GetMetadata("Value"))))
        {
          task.LogError("Invalid parameters. NuSpecPropValues must be an array of Property and Value. "
                        + NuSpecPropValuesUsage);
          return false;
        }

        foreach (var nspv in nuSpecPropValues)
          propValues[nspv.GetMetadata("Property")] = new PropertyEdit(nspv.GetMetadata("Value"), nspv.GetMetadata("EditMode"));
      }

      else if (nuSpecProperty != null && nuSpecValue != null)
      {
        if (string.IsNullOrWhiteSpace(nuSpecProperty))
        {
          task.LogError("Invalid parameters. MyNuSpecProperty cannot be empty. " + NuSpecSinglePropValueUsage);
          return false;
        }

        propValues[nuSpecProperty] = new PropertyEdit(nuSpecValue, editMode);
      }

      else if (failIfEmpty)
      {
        task.LogError(
          $"Invalid parameters. Provide at least one of NuSpecPropValues or (NuSpecProperty and NuSpecValue) parameter.\n\n{NuSpecSinglePropValueUsage}\n\n{NuSpecPropValuesUsage}");
        return false;
      }

      else
      {
        task.LogInfo($"No content provided to write to {nuSpecFilePath}. Exiting.");
        return true;
      }

      if (File.Exists(nuSpecFilePath) == false)
      {
        task.LogError($"Could not find file {nuSpecFilePath}, make sure it exists and its permissions are correct.");
        return false;
      }

      var doc     = XDocument.Load(nuSpecFilePath);
      var package = doc.Root;

      if (package == null || package.Name.LocalName != "package")
      {
        task.LogError($"Invalid NuSpec file '{nuSpecFilePath}': <package> does not exist, got {package?.Name} instead");
        return false;
      }

      var ns      = doc.Root.Name.Namespace;
      var section = package.Element(ns + nuSpecSection);

      if (section == null)
      {
        task.LogError($"Invalid section '{nuSpecSection}': section does not exist");
        return false;
      }

      foreach (var propValue in propValues)
      {
        XElement prop = section.Element(ns + propValue.Key);

        if (prop == null)
          section.Add(prop = new XElement(ns + propValue.Key));

        switch (propValue.Value.EditMode)
        {
          case "Append":
            prop.Value += propValue.Value.Value;
            break;

          case "AppendLine":
            prop.Value += "\\n" + propValue.Value.Value;
            break;

          case "Insert":
            prop.Value = propValue.Value.Value + prop.Value;
            break;

          case "InsertLine":
            prop.Value = propValue.Value.Value + "\\n" + prop.Value;
            break;

          case "Replace":
          default:
            prop.Value = propValue.Value.Value;
            break;
        }
      }

      doc.Save(nuSpecFilePath);

      return true;
    }
  }
}
