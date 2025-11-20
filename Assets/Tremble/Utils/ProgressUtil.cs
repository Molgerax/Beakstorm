//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

//#define USE_SYNC_PROGRESS

using UnityEditor;

namespace TinyGoose.Tremble
{
	public static class ProgressUtil
	{
		public static int Start(string taskName)
		{
#if UNITY_EDITOR
	#if USE_SYNC_PROGRESS
			EditorUtility.DisplayProgressBar("Trembling...", taskName, 0f);
			return -1;
	#else
			return Progress.Start(taskName, options: Progress.Options.Managed | Progress.Options.Sticky);
	#endif
#else
			return -1;
#endif
		}

		public static void Succeed(int task)
		{
#if UNITY_EDITOR
	#if USE_SYNC_PROGRESS
			EditorUtility.DisplayProgressBar("Trembling...", "done", 1f);
			EditorUtility.ClearProgressBar();
	#else
			if (task == 0)
				return;
			Progress.Finish(task);
	#endif
#endif
		}

		public static void Fail(int task)
		{
#if UNITY_EDITOR
	#if USE_SYNC_PROGRESS
			EditorUtility.ClearProgressBar();
	#else
			if (task == 0)
				return;
			Progress.Finish(task, Progress.Status.Failed);
	#endif
#endif
		}

		public static void Report(int task, int step, int totalSteps, string description)
		{
#if UNITY_EDITOR
	#if USE_SYNC_PROGRESS
			float progress = (float)step / (float)totalSteps;
			EditorUtility.DisplayProgressBar("Tremble", $"{description} ({step + 1}/{totalSteps} - {progress*100:F0}%)", progress);
	#else
			if (task == 0)
				return;

			float progress = (float)step / (float)totalSteps;
			Progress.Report(task, step, totalSteps, $"{description} ({step + 1}/{totalSteps} - {progress*100:F0}%)");
	#endif
#endif
		}
	}
}