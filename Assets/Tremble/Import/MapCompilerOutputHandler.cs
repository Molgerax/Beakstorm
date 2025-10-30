//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using AOT;
using UnityEngine;

namespace TinyGoose.Tremble
{
	// This class encapsulates the logic of parsing messages from Q3Map2, passing them on and
	// retaining any overrides to the end result.
	//
	// It used to be a nice lambda in MapCompiler.cs, but IL2CPP does not support instance method callbacks
	// from native code :(
	public static class MapCompilerOutputHandler
	{
		private static Action<string> s_OnOutputMessage;
		private static Q3Map2Result? s_OverrideResult;

		public static void Init(Action<string> onOutputMessage)
		{
			s_OnOutputMessage = onOutputMessage;
			s_OverrideResult = null;
		}

		public static Q3Map2Result? DeInit()
		{
			s_OnOutputMessage = null;
			return s_OverrideResult;
		}

		[MonoPInvokeCallback(typeof(Action<string>))]
		public static void HandleOutput(string message)
		{
			if (message.ContainsInvariant("Couldn't find image"))
			{
				if (!message.ContainsInvariant("__TB") &&
				   !message.ContainsInvariant("special/") &&
					!message.ContainsInvariant(TBConsts.CLIP_TEXTURE) &&
					!message.ContainsInvariant(TBConsts.SKIP_TEXTURE) &&
					!message.ContainsInvariant(TBConsts.TRIGGER_TEXTURE))
				{
					s_OverrideResult = Q3Map2Result.FailedWithMissingTextures;
				}
			}
			else if (message.StartsWithInvariant("WARNING:") &&
			         !message.ContainsInvariant("Unknown option") &&
			         !message.ContainsInvariant("90 percent structural map"))
			{
				Debug.LogWarning($"Q3Map2: {message}");
				s_OverrideResult = Q3Map2Result.SucceededWithWarnings;
			}

			s_OnOutputMessage?.Invoke(message);
		}
	}
}