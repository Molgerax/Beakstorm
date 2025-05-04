using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// A fix for Cloud Build while using Wwise. Add to Editor Folder
/// Based on solution from Blackbox Relaities: https://github.com/BlackBoxSimulations/wwise-unity-cloud-build-fixer
/// </summary>
public class WwiseFixEditor : MonoBehaviour
{
  class WwiseReplacement
  {
    private string Original;
    private string Replacement;

    public WwiseReplacement(string original, string replacement)
    {
      Original = original;
      Replacement = replacement;
    }

    public bool ReplaceIfNecessary(string input, out string output)
    {
      if (input.Equals(Original, StringComparison.OrdinalIgnoreCase))
      {
        output = Replacement;
        return true;
      }
      else if (input.Contains(Original))
      {
        output = input.Replace(Original, Replacement);
        return true;
      }
      else
      {
        output = null;
        return false;
      }
    }

    public bool ReverseReplaceIfNecessary(string input, out string output)
    {
      if (input.Equals(Replacement, StringComparison.OrdinalIgnoreCase))
      {
        output = Original;
        return true;
      }
      else if (input.Contains(Replacement))
      {
        output = input.Replace(Replacement, Original);
        return true;
      }
      else
      {
        output = null;
        return false;
      }
    }
  }

  private static WwiseReplacement[] WwisePatches =
  {
    // Mac
    new WwiseReplacement("#if (UNITY_STANDALONE_OSX && !UNITY_EDITOR) || UNITY_EDITOR_OSX",
      "#if (UNITY_STANDALONE_OSX && !UNITY_EDITOR) || (UNITY_EDITOR_OSX && !UNITY_STANDALONE_WIN)"),
    // Windows
    new WwiseReplacement("#if (UNITY_STANDALONE_WIN && !UNITY_EDITOR) || UNITY_EDITOR_WIN",
      "#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA"),
    new WwiseReplacement("#if UNITY_EDITOR_WIN || (UNITY_STANDALONE_WIN && !UNITY_EDITOR)",
      "#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN || UNITY_WSA"),
    //Linux
    new WwiseReplacement("#if UNITY_STANDALONE_LINUX && ! UNITY_EDITOR", "#if UNITY_EDITOR_LINUX || (UNITY_STANDALONE_LINUX && !UNITY_EDITOR)"),
    new WwiseReplacement("#if UNITY_STANDALONE_LINUX && !UNITY_EDITOR", "#if UNITY_EDITOR_LINUX || (UNITY_STANDALONE_LINUX && !UNITY_EDITOR)"),
    new WwiseReplacement("#if (UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_WEBGL) && !UNITY_EDITOR", "#if ((UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_WEBGL) && !UNITY_EDITOR) || UNITY_EDITOR_LINUX"),
    // Android
    new WwiseReplacement("#if UNITY_ANDROID && !UNITY_EDITOR",
      "#if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_ANDROID && UNITY_EDITOR_LINUX)"),
    new WwiseReplacement("#if UNITY_ANDROID && ! UNITY_EDITOR",
      "#if (UNITY_ANDROID && !UNITY_EDITOR) || (UNITY_ANDROID && UNITY_EDITOR_LINUX)"),
    // iOS
    new WwiseReplacement("#if (UNITY_IOS && !UNITY_EDITOR) || (UNITY_IOS && UNITY_EDITOR_LINUX)",
      "#if UNITY_IOS && !UNITY_EDITOR"),
    //new WwiseReplacement("#if UNITY_IOS && ! UNITY_EDITOR", "#if (UNITY_IOS && !UNITY_EDITOR) || (UNITY_IOS && UNITY_EDITOR_LINUX)"),
    new WwiseReplacement("#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR",
      "#if (UNITY_IOS && !UNITY_EDITOR) || (UNITY_IOS && UNITY_EDTIOR_LINUX)"),
    new WwiseReplacement(
      "#if (UNITY_IOS && !UNITY_EDITOR) || (UNITY_IOS && UNITY_EDTIOR_LINUX) || (UNITY_TVOS && !UNITY_EDITOR) || (UNITY_TVOS && UNITY_EDITOR_LINUX)",
      "#if (UNITY_IOS && !UNITY_EDITOR) || (UNITY_IOS && UNITY_EDTIOR_LINUX)"),
    // Switch
    new WwiseReplacement(
      "#if (UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_STADIA) && !UNITY_EDITOR",
      "#if (UNITY_SWITCH || UNITY_ANDROID || UNITY_STANDALONE_LINUX || UNITY_STADIA) && (!UNITY_EDITOR || UNITY_EDITOR_LINUX)")
  };

  [MenuItem("Tools/WwiseFixer/Apply Fix")]
  public static void ApplyWwiseScriptsFix()
  {
    ModifyWwiseScriptsTempFix(false);
  }

  [MenuItem("Tools/WwiseFixer/Undo Fix")]
  public static void RemoveWwiseScriptsFix()
  {
    ModifyWwiseScriptsTempFix(true);
  }

  private static void ModifyWwiseScriptsTempFix(bool undo)
  {
    string path = EditorUtility.OpenFolderPanel("Wwise Root Folder in Assets", "", "");
    GetDirectories(path, undo);
    AssetDatabase.Refresh();
  }

  public static void GetDirectories(string path, bool undo)
  {
    string[] directories = Directory.GetDirectories(path);
    ReplaceFiles(Directory.GetFiles(path), path, undo);

    foreach (string directory in directories)
      GetDirectories(directory, undo);
  }

  private static void ReplaceFiles(string[] files, string path, bool undo)
  {
    foreach (string file in files)
    {
      if (file.EndsWith(".cs"))
      {
        var text = File.ReadAllText(file);
        var split = text.Split(Environment.NewLine.ToCharArray());
        Debug.Log(String.Join("|", Environment.NewLine.ToCharArray()));

        for (int i = 0; i < split.Length; i++)
        {
          foreach (WwiseReplacement patch in WwisePatches)
          {
            if (undo)
            {
              if (patch.ReverseReplaceIfNecessary(split[i], out string result))
              {
                // Stop trying to replace things after the first replacement occurs
                split[i] = result;
                break;
              }
            }
            else
            {
              if (patch.ReplaceIfNecessary(split[i], out string result))
              {
                // Stop trying to replace things after the first replacement occurs
                split[i] = result;
                break;
              }
            }
          }
        }

        List<string> nonBlankLines = new List<string>();
        foreach (string line in split)
        {
          if (!string.IsNullOrWhiteSpace(line))
            nonBlankLines.Add(line);
        }

        File.WriteAllLines(file, nonBlankLines);
      }
    }
  }

  [MenuItem("Tools/WwiseFixer/Set Release Setting/True")]
  public static void SetReleaseSetting()
  {
    AkPluginActivator.ActivatePluginsForDeployment(BuildTarget.StandaloneWindows64, true);
  }
  
  [MenuItem("Tools/WwiseFixer/Set Release Setting/False")]
  public static void SetReleaseSettingFalse()
  {
    AkPluginActivator.ActivatePluginsForDeployment(BuildTarget.StandaloneWindows64, false);
  }
}