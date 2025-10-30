//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	public record EnumDefinition(string Name, string[] Values);

	public static class FgdParser
	{
		public static void Import(string fgdFilePath)
		{
			string fgdName = Path.GetFileNameWithoutExtension(fgdFilePath).ToNamingConvention(NamingConvention.UpperCamelCase);

			TokenParser tokenParser = new(File.ReadAllText(fgdFilePath));

			List<FgdClass> baseClasses = new(16);
			List<FgdClass> concreteClasses = new(64);

			List<EnumDefinition> enums = new(64);

			// Work out type of class and parse
			while (!tokenParser.IsAtEnd)
			{
				string classType = tokenParser.ReadToken(eatAllBracketContent: true).ToString();

				switch (classType.ToLowerInvariant())
				{
					case "@baseclass":
						baseClasses.Add(ParseClass(FgdClassType.Base, tokenParser, fgdName, enums));
						break;
					case "@pointclass":
						concreteClasses.Add(ParseClass(FgdClassType.Point, tokenParser, fgdName, enums));
						break;
					case "@solidclass":
						concreteClasses.Add(ParseClass(FgdClassType.Brush, tokenParser, fgdName, enums));
						break;
					default:
						continue;
				}
			}

			string basePath = Path.GetDirectoryName(fgdFilePath);
			string baseEnumPath = Path.Combine(basePath, fgdName, "Enums");
			string baseEntitiesPath = Path.Combine(basePath, fgdName, "Entities");

			DirectoryUtil.CreateAllDirectories(baseEnumPath);
			DirectoryUtil.CreateAllDirectories(baseEntitiesPath);

			// Write Enums first
			foreach (EnumDefinition enumDef in enums)
			{
				string enumFile = Path.Combine(baseEnumPath, enumDef.Name + ".cs");
				enumDef.WriteCSharp(enumFile, fgdName);
			}

			// ... then base classes ...
			foreach (FgdClass baseClass in baseClasses)
			{
				string namespaceName = fgdName;
				string folderPath = baseEntitiesPath;

				baseClass.Name.Split('_', out string folderName, out string bareClassName);
				if (!folderName.IsNullOrEmpty())
				{
					namespaceName = $"{fgdName.ToNamingConvention(NamingConvention.UpperCamelCase)}.{folderName.ToNamingConvention(NamingConvention.UpperCamelCase)}";
					folderPath = Path.Combine(folderPath, folderName);
				}

				DirectoryUtil.CreateAllDirectories(folderPath);

				string classFile = Path.Combine(folderPath, "I" + bareClassName.ToNamingConvention(NamingConvention.UpperCamelCase) + ".cs");
				baseClass.WriteCSharp(classFile, baseClasses, namespaceName);
			}

			// ... finally concrete classes
			foreach (FgdClass concreteClass in concreteClasses)
			{
				string namespaceName = fgdName;
				string folderPath = baseEntitiesPath;

				concreteClass.Name.Split('_', out string folderName, out string bareClassName);
				if (!folderName.IsNullOrEmpty())
				{
					namespaceName = $"{fgdName.ToNamingConvention(NamingConvention.UpperCamelCase)}.{folderName.ToNamingConvention(NamingConvention.UpperCamelCase)}";
					folderPath = Path.Combine(folderPath, folderName);
				}

				DirectoryUtil.CreateAllDirectories(folderPath);

				string classFile = Path.Combine(folderPath, bareClassName.ToNamingConvention(NamingConvention.UpperCamelCase) + ".cs");
				concreteClass.WriteCSharp(classFile, baseClasses, namespaceName);
			}

			// Refresh code, in editor
#if UNITY_EDITOR
			AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
#endif
		}

		private static FgdClass ParseClass(FgdClassType type, TokenParser tokenParser, string fgdNamespace, List<EnumDefinition> outEnumDefinitions)
		{
			Dictionary<string, string> metadata = new();
			while (!tokenParser.PeekToken("=", eatAllBracketContent: true))
			{
				string metadataString = tokenParser.ReadToken(eatAllBracketContent: true).ToString(); // e.g. base(asfasf, asdgs) or model({f: "fasf"})
				metadataString.Split('(', out string key, out string value);

				metadata[key.ToLowerInvariant()] = value[..^1];
			}

			tokenParser.MatchToken("=", eatAllBracketContent: true);

			ReadOnlySpan<char> classname = tokenParser.ReadToken();
			string classDescription = null;

			classname.Split('_', out string _, out string unityClassname);
			unityClassname = unityClassname.ToNamingConvention(NamingConvention.UpperCamelCase);

			if (tokenParser.PeekToken(":"))
			{
				tokenParser.ReadToken();
				classDescription = tokenParser.ReadToken().ToString();
			}

			FgdClass newClass = new(type, classname.ToString(), classDescription ?? $"{unityClassname} Class");

			// Add base classes
			if (metadata.TryGetValue("base", out string bases))
			{
				foreach (string baseClass in bases.Split(","))
				{
					newClass.AddBaseClass(baseClass.Trim());
				}
			}

			//TODO(jwf): what about model, colour, size, etc?

			if (!tokenParser.PeekToken("[]"))
			{
				tokenParser.MatchToken("["); // Eat '['

				while (!tokenParser.IsAtEnd && !tokenParser.PeekToken("]"))
				{
					// Get name and type
					ReadOnlySpan<char> typeString = tokenParser.ReadToken(eatAllBracketContent: true); // e.g. thing(Integer)
					typeString.Split('(', out string varName, out string typename);
					typename = typename[..^1];

					string description = null;
					string defaultValue = null;

					// Has description?
					if (tokenParser.PeekToken(":"))
					{
						tokenParser.MatchToken(":");
						description = tokenParser.ReadToken().ToString();
					}

					// Has default value?
					if (tokenParser.PeekToken(":"))
					{
						tokenParser.MatchToken(":");
						defaultValue = tokenParser.ReadToken().ToString();
					}

					// Has multiple selections?
					if (tokenParser.PeekToken("="))
					{
						tokenParser.MatchToken("=");
						tokenParser.MatchToken("[");

						if (typename.EqualsInvariant("choices"))
						{
							// Collect values (not necessarily in order, not necessarily sequential)
							Dictionary<int, string> values = new(64);
							int counter = 0;

							while (!tokenParser.PeekToken("]"))
							{
								if (!int.TryParse(tokenParser.ReadToken(supportCommentsAndQuotes: false), NumberStyles.Any, CultureInfo.InvariantCulture, out int enumValue))
								{
									enumValue = counter++;
								}
								tokenParser.MatchToken(":");
								string enumValueName = NamingConventionUtil.FromHuman(tokenParser.ReadToken());
								values[enumValue] = enumValueName;
							}

							tokenParser.MatchToken("]");

							int maxEnumValue = values.Keys.Max();
							string[] valueStrings = new string[maxEnumValue+1];
							for (int i = 0; i <= maxEnumValue; i++)
							{
								valueStrings[i] = values.TryGetValue(i, out string valueName) ? valueName : $"Value_{i}";
							}

							string enumName = $"{unityClassname.ToNamingConvention(NamingConvention.UpperCamelCase)}{varName.ToNamingConvention(NamingConvention.UpperCamelCase)}";
							outEnumDefinitions.Add(new
							(
								Name: enumName,
								Values: valueStrings
							));

							newClass.AddField(new FgdEnumField
							{
								Name = varName,
								Description = description,
								EnumClassName = $"{fgdNamespace}.{enumName}",
								DefaultValue = 0,
								Values = valueStrings,
							});
						}
						else if (typename.EqualsInvariant("flags"))
						{
							while (!tokenParser.PeekToken("]"))
							{
								int flagBit = (int)Math.Log(int.Parse(tokenParser.ReadToken(), NumberStyles.Any, CultureInfo.InvariantCulture), 2);
								tokenParser.MatchToken(":");
								ReadOnlySpan<char> flagName = tokenParser.ReadToken();
								int flagDefaultValue = 0;

								if (tokenParser.PeekToken(":"))
								{
									tokenParser.MatchToken(":");
									flagDefaultValue = int.Parse(tokenParser.ReadToken(), NumberStyles.Any, CultureInfo.InvariantCulture);
								}

								newClass.AddField(new FgdSpawnFlagField
								{
									Bit = flagBit,
									Description = flagName.ToString(),
									DefaultValue = flagDefaultValue == 1
								});
							}
						}
						else
						{
							Debug.LogWarning("Unexpected options list... skipping...");

							while (!tokenParser.PeekToken("]"))
							{
								tokenParser.ReadToken();
							}
						}
					}
					else
					{
						FgdFieldBase field = FgdFieldFactory.CreateField(typename, varName, description, defaultValue);
						if (field == null)
						{
							Debug.LogWarning($"Could not parse field '{varName}' of type '{typename}'");
							continue;
						}

						newClass.AddField(field);
					}
				}
			}
			else
			{
				// Eat "[]"
				tokenParser.MatchToken("[]");
			}

			return newClass;
		}
	}
}