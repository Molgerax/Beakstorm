//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
	public static class EditorCompileUtil
	{
		private static readonly EditorPrefsBackedString s_CompileList = new("Tremble_PostCompileActions");

		/// <summary>
		/// Compile scripts, then call a static method.
		/// Method MUST be static.
		/// Method can be private or public.
		/// Method must take 0 args, or any number of STRING args only.
		/// </summary>
		/// <param name="type">Type containing method</param>
		/// <param name="staticFunctionName">Method name (use nameof)</param>
		/// <param name="stringArgs">Optional list of STRING only args to pass (must not contain ',')</param>
		public static void CompileThen(Type type, string staticFunctionName, params string[] stringArgs)
		{
			foreach (string stringArg in stringArgs)
			{
				if (stringArg.Contains(','))
				{
					Debug.LogError($"Cannot schedule call to {type.Name}.{staticFunctionName} - arg contains ',' :(");
					return;
				}
			}

			// Add to compile list, backed by editor prefs
			string[] existing = s_CompileList.Values;
			string[] newList = new string[existing.Length + 1];

			if (existing.Length > 0)
			{
				Array.Copy(existing, newList, existing.Length);
			}

			newList[^1] = $"{type.FullName}.{staticFunctionName}({String.Join(',', stringArgs)})";
			s_CompileList.Values = newList;

			// Now recompile
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		private static void PostCompile()
		{
			try
			{
				// Get list of calls and call each one
				string[] calls = s_CompileList.Values;
				foreach (string call in calls)
				{
					RunCall(call);
				}
			}
			finally
			{
				// Finally clear!
				s_CompileList.Values = null;
			}
		}

		private static void RunCall(string call)
		{
			Match match = Regex.Match(call, @"(.+)\.(.+?)\((.+?)?\)");
			if (match.Groups.Count < 4)
			{
				Debug.LogError($"Call '{call}' was not in the correct format :(");
				return;
			}

			string className = match.Groups[1].Captures[0].Value;
			string methodName = match.Groups[2].Captures[0].Value;

			string argString = match.Groups[3].Captures.Count > 0 ? match.Groups[3].Captures[0].Value : null;
			string[] args = argString.IsNullOrEmpty() ? Array.Empty<string>() : argString.Split(',');

			Type finalType = Unsupported.GetTypeFromFullName(className);
			if (finalType == null)
			{
				Debug.LogError($"Couldn't find type {className}!");
				return;
			}

			MethodInfo method = finalType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (method == null)
			{
				Debug.LogError($"Couldn't find static method {finalType.Name}.{methodName}!");
				return;
			}

			int numDeclaringParams = method.GetParameters().Length;
			if (numDeclaringParams != args.Length)
			{
				Debug.LogError($"Couldn't call static method {finalType.Name}.{methodName} with {args.Length} args (expected: {numDeclaringParams}!");
				return;
			}

			method.Invoke(null, args.Select(s => (object)s).ToArray());
		}
	}
}