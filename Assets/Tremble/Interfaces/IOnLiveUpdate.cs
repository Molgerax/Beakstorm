//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

namespace TinyGoose.Tremble
{
	// Custom LiveUpdate
	public interface IOnLiveUpdate
	{
		void OnLiveUpdated();
	}

	// Simple LiveUpdate - just call OnDestroy followed by Start!
	public interface IOnLiveUpdate_Simple
	{
	}
}