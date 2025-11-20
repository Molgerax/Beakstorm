// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

// This allows you to use properties with init-only setters,
// e.g. public int MyProperty { get; init; }
namespace System.Runtime.CompilerServices
{
	public struct IsExternalInit {}   
}