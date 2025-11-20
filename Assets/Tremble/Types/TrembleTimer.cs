//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TinyGoose.Tremble
{
	[Serializable]
	public struct StoredTrembleTimer
	{
		public string Context;
		public long LastMillis;
	}

	public struct TrembleTimerScope : IDisposable
	{
		private readonly TrembleTimer m_Timer;
		private bool m_Disposed;

		public TrembleTimerScope(Type type)
		{
			m_Timer = TrembleTimer.GetTimer(type);
			m_Timer.BeginCapture();

			m_Disposed = false;
		}
		public TrembleTimerScope(TrembleTimer.Context context)
		{
			m_Timer = TrembleTimer.GetTimer(context);
			m_Timer.BeginCapture();

			m_Disposed = false;
		}

		public void Dispose()
		{
			if (m_Disposed)
				return;

			m_Disposed = true;
			m_Timer.EndCapture();
		}
	}

	public class TrembleTimer
	{
		public enum Context
		{
			// Import
			ExportMapToStaging,
			RunQ3Map2,
			ParseBsp,
			CreateGameObjects,
			SplitMesh,
			SmoothMeshNormals,
			SimplifyCollisionMeshes,
			GenerateUV2,
			RunMapProcessors,

			// Sync
			CreateTypeLookups,
			ExportEntities,
			GatherDataAssets,
			ExportMaterials,
			WriteEntitiesFgdAndGameConfig,
			Cleanup,
			ExportAddressableDataForStandalone,
		}

		private static readonly Dictionary<string, TrembleTimer> s_CurrentTimers = new();
		private static string s_SessionContext;
		private static DateTime s_SessionStart;

		private DateTime m_CaptureStart;
		private long m_TotalMilliseconds;

		public long TotalMilliseconds => m_TotalMilliseconds;

		public static void BeginSession(string sessionContext)
		{
			s_SessionStart = DateTime.Now;
			s_SessionContext = sessionContext;
			s_CurrentTimers.Clear();
		}

		public static void EndSession()
		{
			if (TrembleSyncSettings.Get().LogOnImportAndSync)
			{
				float seconds = (float)(DateTime.Now - s_SessionStart).TotalSeconds;
				Debug.Log($"{s_SessionContext} complete in {seconds:F3}s!");
			}

			foreach ((string name, TrembleTimer timer) in s_CurrentTimers)
			{
#if UNITY_EDITOR
				EditorPrefs.SetInt($"TrembleTimer_{name}", (int)timer.TotalMilliseconds);
#endif

				if (TrembleSyncSettings.Get().LogOnImportAndSync)
				{
					Debug.Log($"    {name} took {timer.TotalMilliseconds}ms");
				}
			}
		}

		public static TrembleTimer GetTimer(Type processorType) => GetTimer(processorType.FullName);
		public static TrembleTimer GetTimer(Context context) => GetTimer(context.ToString());

		private static TrembleTimer GetTimer(string name)
		{
			if (!s_CurrentTimers.TryGetValue(name, out TrembleTimer timer))
			{
				timer = new();
				s_CurrentTimers[name] = timer;
			}

			return timer;
		}

		public static long GetLastTime(Type processorType) => GetLastTime(processorType.FullName);
		public static long GetLastTime(Context context) => GetLastTime(context.ToString());

		private static long GetLastTime(string name)
#if UNITY_EDITOR
			=> EditorPrefs.GetInt($"TrembleTimer_{name}", -1);
#else
			=> 0L;
#endif


		public void BeginCapture()
		{
			m_CaptureStart = DateTime.Now;
		}
		public void EndCapture()
		{
			m_TotalMilliseconds += (long)(DateTime.Now - m_CaptureStart).TotalMilliseconds;
		}
	}
}