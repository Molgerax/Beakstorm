//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

//#define USE_TYPEDB_IN_EDITOR

using System;
using System.Collections.Generic;

#if !UNITY_EDITOR || USE_TYPEDB_IN_EDITOR
using System.Linq;
using UnityEngine;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif


namespace TinyGoose.Tremble
{
	public static class TypeDatabase
	{
		private static Dictionary<string, Type> s_ClassLookup;
		private static Dictionary<Type, object[]> s_ClassAttributes;

		public static Type GetType(string fullName)
		{
			if (fullName == null)
				return null;

#if UNITY_EDITOR && !USE_TYPEDB_IN_EDITOR
			// Unsupported - but very fast!
			return Unsupported.GetTypeFromFullName(fullName);
#else
			if (s_ClassLookup == null)
			{
				GenerateClassLookup();
			}

			return s_ClassLookup.GetValueOrDefault(fullName);
#endif
		}

		public static IList<Type> GetTypesWithAttribute<TAttribute>()
			where TAttribute : Attribute
		{
#if UNITY_EDITOR && !USE_TYPEDB_IN_EDITOR
			return TypeCache.GetTypesWithAttribute<TAttribute>();
#else
			if (s_ClassLookup == null)
			{
				GenerateClassLookup();
			}

			List<Type> types = new(256);
			foreach (KeyValuePair<Type, object[]> kvp in s_ClassAttributes)
			{
				foreach (object attribute in kvp.Value)
				{
					Type attributeType = attribute.GetType();

					if (attributeType == typeof(TAttribute) || attributeType.IsSubclassOf(typeof(TAttribute)))
					{
						types.Add(kvp.Key);
					}
				}
			}

			return types;
#endif
		}


#if !UNITY_EDITOR || USE_TYPEDB_IN_EDITOR
		private static readonly HashSet<string> FORBIDDEN_ASSEMBLIES = new()
		{
			"mscorlib",
			"UnityEngine",
			"UnityEngine.AIModule",
			"UnityEngine.AMDModule",
			"UnityEngine.ARModule",
			"UnityEngine.AccessibilityModule",
			"UnityEngine.AndroidJNIModule",
			"UnityEngine.AnimationModule",
			"UnityEngine.AssetBundleModule",
			"UnityEngine.AudioModule",
			"UnityEngine.ClothModule",
			"UnityEngine.ClusterInputModule",
			"UnityEngine.ClusterRendererModule",
			"UnityEngine.ContentLoadModule",
			"UnityEngine.CoreModule",
			"UnityEngine.CrashReportingModule",
			"UnityEngine.DSPGraphModule",
			"UnityEngine.DirectorModule",
			"UnityEngine.GIModule",
			"UnityEngine.GameCenterModule",
			"UnityEngine.GraphicsStateCollectionSerializerModule",
			"UnityEngine.GridModule",
			"UnityEngine.HierarchyCoreModule",
			"UnityEngine.HotReloadModule",
			"UnityEngine.IMGUIModule",
			"UnityEngine.ImageConversionModule",
			"UnityEngine.InputModule",
			"UnityEngine.InputForUIModule",
			"UnityEngine.InputLegacyModule",
			"UnityEngine.JSONSerializeModule",
			"UnityEngine.LocalizationModule",
			"UnityEngine.MarshallingModule",
			"UnityEngine.MultiplayerModule",
			"UnityEngine.NVIDIAModule",
			"UnityEngine.ParticleSystemModule",
			"UnityEngine.PerformanceReportingModule",
			"UnityEngine.PhysicsModule",
			"UnityEngine.Physics2DModule",
			"UnityEngine.PropertiesModule",
			"UnityEngine.RuntimeInitializeOnLoadManagerInitializerModule",
			"UnityEngine.ScreenCaptureModule",
			"UnityEngine.ShaderVariantAnalyticsModule",
			"UnityEngine.SharedInternalsModule",
			"UnityEngine.SpriteMaskModule",
			"UnityEngine.SpriteShapeModule",
			"UnityEngine.StreamingModule",
			"UnityEngine.SubstanceModule",
			"UnityEngine.SubsystemsModule",
			"UnityEngine.TLSModule",
			"UnityEngine.TerrainModule",
			"UnityEngine.TerrainPhysicsModule",
			"UnityEngine.TextCoreFontEngineModule",
			"UnityEngine.TextCoreTextEngineModule",
			"UnityEngine.TextRenderingModule",
			"UnityEngine.TilemapModule",
			"UnityEngine.UIModule",
			"UnityEngine.UIElementsModule",
			"UnityEngine.UmbraModule",
			"UnityEngine.UnityAnalyticsModule",
			"UnityEngine.UnityAnalyticsCommonModule",
			"UnityEngine.UnityConnectModule",
			"UnityEngine.UnityCurlModule",
			"UnityEngine.UnityTestProtocolModule",
			"UnityEngine.UnityWebRequestModule",
			"UnityEngine.UnityWebRequestAssetBundleModule",
			"UnityEngine.UnityWebRequestAudioModule",
			"UnityEngine.UnityWebRequestTextureModule",
			"UnityEngine.UnityWebRequestWWWModule",
			"UnityEngine.VFXModule",
			"UnityEngine.VRModule",
			"UnityEngine.VehiclesModule",
			"UnityEngine.VideoModule",
			"UnityEngine.VirtualTexturingModule",
			"UnityEngine.WindModule",
			"UnityEngine.XRModule",
			"UnityEditor",
			"UnityEditor.AccessibilityModule",
			"UnityEditor.AdaptivePerformanceModule",
			"UnityEditor.BuildProfileModule",
			"UnityEditor.CoreBusinessMetricsModule",
			"UnityEditor.CoreModule",
			"UnityEditor.DeviceSimulatorModule",
			"UnityEditor.DiagnosticsModule",
			"UnityEditor.EditorToolbarModule",
			"UnityEditor.GIModule",
			"UnityEditor.GraphViewModule",
			"UnityEditor.GraphicsStateCollectionSerializerModule",
			"UnityEditor.GridAndSnapModule",
			"UnityEditor.GridModule",
			"UnityEditor.MultiplayerModule",
			"UnityEditor.Physics2DModule",
			"UnityEditor.PhysicsModule",
			"UnityEditor.PresetsUIModule",
			"UnityEditor.PropertiesModule",
			"UnityEditor.QuickSearchModule",
			"UnityEditor.SafeModeModule",
			"UnityEditor.SceneTemplateModule",
			"UnityEditor.SceneViewModule",
			"UnityEditor.ShaderFoundryModule",
			"UnityEditor.SketchUpModule",
			"UnityEditor.SpriteMaskModule",
			"UnityEditor.SpriteShapeModule",
			"UnityEditor.SubstanceModule",
			"UnityEditor.TerrainModule",
			"UnityEditor.TextCoreFontEngineModule",
			"UnityEditor.TextCoreTextEngineModule",
			"UnityEditor.TextRenderingModule",
			"UnityEditor.TilemapModule",
			"UnityEditor.TreeModule",
			"UnityEditor.UIAutomationModule",
			"UnityEditor.UIBuilderModule",
			"UnityEditor.UIElementsModule",
			"UnityEditor.UIElementsSamplesModule",
			"UnityEditor.UmbraModule",
			"UnityEditor.UnityConnectModule",
			"UnityEditor.VFXModule",
			"UnityEditor.VideoModule",
			"UnityEditor.XRModule",
			"netstandard",
			"System.Core",
			"System",
			"Mono.Security",
			"System.Configuration",
			"System.Xml",
			"System.Numerics",
			"System.Data",
			"System.Transactions",
			"System.EnterpriseServices",
			"System.Runtime.Serialization",
			"System.ServiceModel.Internals",
			"System.Xml.Linq",
			"Unity.Cecil",
			"Bee.BeeDriver2",
			"Bee.BinLog",
			"UnityEditor.Graphs",
			"UnityEditor.OSXStandalone.Extensions",
			"UnityEditor.iOS.Extensions.Xcode",
			"Autodesk.Fbx.BuildTestAssets",
			"Autodesk.Fbx",
			"Autodesk.Fbx.Editor",
			"PPv2URPConverters",
			"Unity.Addressables",
			"Unity.Addressables.Editor",
			"Unity.AI.Navigation",
			"Unity.AI.Navigation.Editor.ConversionSystem",
			"Unity.AI.Navigation.Editor",
			"Unity.AI.Navigation.Updater",
			"Unity.Bindings.OpenImageIO.Editor",
			"Unity.Burst.CodeGen",
			"Unity.Burst",
			"Unity.Burst.Editor",
			"Unity.Cinemachine",
			"Unity.Cinemachine.Editor",
			"Unity.CollabProxy.Editor",
			"Unity.Collections.CodeGen",
			"Unity.Collections",
			"Unity.Collections.Editor",
			"Unity.EditorCoroutines.Editor",
			"Unity.Formats.Fbx.Editor",
			"Unity.Formats.Fbx.Runtime",
			"Unity.InputSystem",
			"Unity.InputSystem.ForUI",
			"Unity.InputSystem.TestFramework",
			"Unity.Mathematics",
			"Unity.Mathematics.Editor",
			"Unity.MemoryProfiler",
			"Unity.MemoryProfiler.Editor",
			"Unity.MemoryProfiler.Editor.MemoryProfilerModule",
			"Unity.PerformanceTesting",
			"Unity.PerformanceTesting.Editor",
			"Unity.PlasticSCM.Editor",
			"Unity.Profiling.Core",
			"Unity.Recorder.Base",
			"Unity.Recorder",
			"Unity.Recorder.Editor",
			"Unity.Rendering.LightTransport.Editor",
			"Unity.Rendering.LightTransport.Runtime",
			"Unity.RenderPipeline.Universal.ShaderLibrary",
			"Unity.RenderPipelines.Core.Editor",
			"Unity.RenderPipelines.Core.Editor.Shared",
			"Unity.RenderPipelines.Core.Runtime",
			"Unity.RenderPipelines.Core.Runtime.Shared",
			"Unity.RenderPipelines.Core.ShaderLibrary",
			"Unity.RenderPipelines.GPUDriven.Runtime",
			"Unity.RenderPipelines.ShaderGraph.ShaderGraphLibrary",
			"Unity.RenderPipelines.Universal.2D.Runtime",
			"Unity.RenderPipelines.Universal.Config.Runtime",
			"Unity.RenderPipelines.Universal.Editor",
			"Unity.RenderPipelines.Universal.Runtime",
			"Unity.RenderPipelines.Universal.Shaders",
			"Unity.ResourceManager",
			"Unity.Rider.Editor",
			"Unity.ScriptableBuildPipeline",
			"Unity.ScriptableBuildPipeline.Editor",
			"Unity.Searcher.Editor",
			"Unity.Serialization",
			"Unity.Serialization.Editor",
			"Unity.Services.Authentication",
			"Unity.Services.Authentication.Editor",
			"Unity.Services.Authentication.Editor.Shared",
			"Unity.Services.Authentication.PlayerAccounts",
			"Unity.Services.Authentication.PlayerAccounts.Editor",
			"Unity.Services.Core.Analytics",
			"Unity.Services.Core.Configuration",
			"Unity.Services.Core.Configuration.Editor",
			"Unity.Services.Core.Device",
			"Unity.Services.Core",
			"Unity.Services.Core.Editor",
			"Unity.Services.Core.Environments",
			"Unity.Services.Core.Environments.Editor",
			"Unity.Services.Core.Environments.Internal",
			"Unity.Services.Core.Internal",
			"Unity.Services.Core.Networking",
			"Unity.Services.Core.Registration",
			"Unity.Services.Core.Scheduler",
			"Unity.Services.Core.Telemetry",
			"Unity.Services.Core.Threading",
			"Unity.Services.Ugc",
			"Unity.Services.Ugc.DocCodeSamples",
			"Unity.Services.Ugc.Internal.Generated",
			"Unity.Settings.Editor",
			"Unity.ShaderGraph.Editor",
			"Unity.ShaderGraph.Utilities",
			"Unity.Splines",
			"Unity.Splines.Editor",
			"Unity.TextMeshPro",
			"Unity.TextMeshPro.Editor",
			"Unity.Timeline",
			"Unity.Timeline.Editor",
			"UnityEditor.TestRunner",
			"UnityEditor.UI",
			"UnityEngine.TestRunner",
			"UnityEngine.UI",
			"Mono.Cecil.Pdb",
			"SingularityGroup.HotReload.RuntimeDependencies",
			"Unity.Burst.Cecil.Pdb",
			"Unity.Collections.LowLevel.ILSupport",
			"nunit.framework",
			"SingularityGroup.HotReload.EditorDependencies",
			"unityplastic",
			"Unity.Plastic.Antlr3.Runtime",
			"Unity.Burst.Cecil.Mdb",
			"Unity.Burst.Cecil",
			"SingularityGroup.HotReload.RuntimeDependencies2020",
			"DOTweenEditor",
			"Mono.Cecil.Mdb",
			"Unity.Plastic.Newtonsoft.Json",
			"Unity.Burst.Unsafe",
			"Mono.Cecil.Rocks",
			"Mono.Cecil",
			"Unity.Burst.Cecil.Rocks",
			"System.Net.Http",
			"Unity.CompilationPipeline.Common",
			"System.Drawing",
			"System.Runtime",
			"Mono.Posix",
			"System.Web",
			"Microsoft.GeneratedCode",
			"JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked",
			"ExCSS.Unity",
			"Microsoft.GeneratedCode",
		};

		public static void GenerateClassLookup()
		{
			s_ClassLookup = new(1024);
			s_ClassAttributes = new(1024);

			IEnumerable<Type> types = AppDomain.CurrentDomain
				.GetAssemblies()
				.Where(ass => !FORBIDDEN_ASSEMBLIES.Contains(ass.GetName().Name))
				.SelectMany(ass => ass.GetTypes())
				.Where(t => t.FullName != null);

			foreach (Type availableType in types)
			{
				s_ClassLookup[availableType.FullName] = availableType;

				if (availableType.IsSubclassOf(typeof(MonoBehaviour)) || availableType.IsSubclassOf(typeof(TrembleFieldConverter)))
				{
					s_ClassAttributes.Add(availableType, availableType.GetCustomAttributes(true));
				}
			}
		}
#endif
	}
}