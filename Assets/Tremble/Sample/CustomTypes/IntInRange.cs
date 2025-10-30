using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TinyGoose.Tremble.Sample
{
	// A custom type representing a random int in a range.
	// See IntInRangeFieldConverter for how this is converted to/from the map!
	
	[Serializable]
	public struct IntInRange
	{
		[SerializeField] private int m_Min;
		[SerializeField] private int m_Max;

		public int Min => m_Min;
		public int Max => m_Max;
		
		public IntInRange(int min, int max)
		{
			m_Min = min;
			m_Max = max;
		}

		public static bool TryParse(string input, out IntInRange value)
		{
			string[] parts = input.Split('-');
			
			// Successfully read from string, but wrong number of parts (expects "0,2" format)
			if (parts.Length != 2)
			{
				value = default;
				return false;
			}

			// We read something like "a,b", but they were not not valid ints!
			if (!int.TryParse(parts[0], out int min) ||
			    !int.TryParse(parts[1], out int max))
			{
				value = default;
				return false;
			}

			// Got it!
			value = new IntInRange(min, max);
			return true;
		}
		public override string ToString() => $"{m_Min}-{m_Max}";

		public int GetRandom() => Random.Range(m_Min, m_Max);
	}
}