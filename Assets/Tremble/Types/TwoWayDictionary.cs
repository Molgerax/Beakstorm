//
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System.Collections.Generic;
using System.IO;

namespace TinyGoose.Tremble
{
	public class TwoWayDictionary<TKey, TValue>
	{
		private readonly Dictionary<TKey, TValue> m_Lookup;
		private readonly Dictionary<TValue, TKey> m_ReverseLookup;

		public TwoWayDictionary()
		{
			m_Lookup = new();
			m_ReverseLookup = new();
		}

		public TwoWayDictionary(int capacity)
		{
			m_Lookup = new(capacity);
			m_ReverseLookup = new(capacity);
		}

		public TValue GetValue(TKey key)
		{
			return m_Lookup[key];
		}
		public TKey GetKey(TValue value)
		{
			return m_ReverseLookup[value];
		}

		public bool ContainsKey(TKey key) => m_Lookup.ContainsKey(key);
		public bool ContainsValue(TValue value) => m_ReverseLookup.ContainsKey(value);

		public bool TryGetValue(TKey key, out TValue value) => m_Lookup.TryGetValue(key, out value);
		public bool TryGetKey(TValue value, out TKey key) => m_ReverseLookup.TryGetValue(value, out key);

		public IEnumerable<TKey> Keys => m_Lookup.Keys;
		public IEnumerable<TValue> Values => m_Lookup.Values;

		public void Clear()
		{
			m_Lookup.Clear();
			m_ReverseLookup.Clear();
		}

		public TValue this[TKey key]
		{
			get => GetValue(key);
			set
			{
				m_Lookup[key] = value;
				m_ReverseLookup[value] = key;
			}
		}

		public void Add(TKey key, TValue value)
		{
			m_Lookup.Add(key, value);
			m_ReverseLookup.Add(value, key);
		}

		public void Remove(TKey key)
		{
			if (m_Lookup.TryGetValue(key, out TValue value))
			{
				m_ReverseLookup.Remove(value);
			}
			m_Lookup.Remove(key);
		}
	}
}