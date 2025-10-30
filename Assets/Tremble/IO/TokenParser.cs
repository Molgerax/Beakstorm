// 
// This file is part of the Tremble package by Tiny Goose.
// Copyright (c) 2024-2025 TinyGoose Ltd., All Rights Reserved.
//

using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace TinyGoose.Tremble
{
	public class TokenParser
	{
		private readonly string m_String;
		private int m_Index;

		public TokenParser(string inString)
		{
			m_String = inString.Replace("\r", ""); // Remove Windows newlines
			m_Index = 0;
		}

		public bool IsAtEnd
		{
			get
			{
				// Fast-forward to next non-whitespace character, if we can
				int index = m_Index;
				while (index < m_String.Length)
				{
					if (m_String[index] is '\n' or '\t')
					{
						index++;
						continue;
					}

					break;
				}

				return (index >= m_String.Length);
			}
		}

#if UNITY_EDITOR
		public string DEBUG_CurrentBuffer
		{
			get
			{
				const int WINDOW_SIZE = 20;
				string before = m_String.Substring(Math.Max(0, m_Index - WINDOW_SIZE), WINDOW_SIZE);
				string after = m_String.Substring(m_Index + 1, Math.Min(m_String.Length - m_Index - 1, WINDOW_SIZE));

				return $"{before} -->{PeekToken().ToString()}<-- {after}";
			}
		}
#endif

		private char Take() => !IsAtEnd ? m_String[m_Index++] : '\n';

		private char Peek() => !IsAtEnd ? m_String[m_Index] : '\n';

		private void Skip() => m_Index++;

		private void FastForwardTo(char target)
		{
			while (!IsAtEnd && m_String[m_Index] != target)
			{
				Skip();
			}
		}

		public ReadOnlySpan<char> ReadToken(bool supportCommentsAndQuotes = true, bool eatAllBracketContent = false, bool disableNumLock = false)
		{
			if (IsAtEnd)
				return "\n";

			// Skip newlines and leading whitespace
			while (Peek() is '\n' or '\t' or ' ')
			{
				Take();
			}

			bool quoteLock = false;
			if (supportCommentsAndQuotes)
			{
				// Handle comment lines:
				// If it's a /, check for // and if found, skip to end of line
				while (Peek() == '/')
				{
					int commentStartIdx = m_Index;
					Skip();

					if (Peek() != '/')
					{
						// No second /? Weird, roll back and continue as normal
						m_Index = commentStartIdx;
						break;
					}

					// Fast-forwarding to EOL...
					FastForwardTo('\n');
					Skip();

					// Skip newlines if found
					while (Peek() == '\n')
					{
						Take();
					}
				}

				// Turn on "quote lock" and skip the quote if the token starts with a quote
				quoteLock = Peek() == '"';
				if (quoteLock)
				{
					Skip();
				}
			}

			int startIdx = m_Index;
			int numChars = 0;
			bool numLock = false;
			int inBrackets = 0; // keep track of whether inside brackets

			while (m_Index < m_String.Length)
			{
				char nextChar = Take();
				numChars++;

				// Turn on numlock when we see a number outside brackets/quotes
				if (!disableNumLock && IsCharNumberFast(nextChar) && inBrackets == 0 && !quoteLock)
				{
					numLock = true;
				}

				// In numlock, but that wasn't a number or "number punctuation"!
				if (numLock && !IsCharNumberFast(nextChar) && nextChar is not '.' and not ',' and not 'e' and not 'E' and not '-' and not '\'')
				{
					if (!IsAtEnd)
					{
						m_Index--; // rewind if not at end!
					}

					break;
				}

				// End of line - that's it!
				if (inBrackets == 0 && nextChar == '\n')
					break;

				if (eatAllBracketContent && nextChar == '(') { inBrackets++; }
				if (eatAllBracketContent && nextChar == ')') { inBrackets--; }

				// Not in quote-lock - found space
				if (inBrackets == 0 && !quoteLock && nextChar == ' ')
					break;

				// We're in quote-lock and we hit the end-quote
				if (nextChar == '"' && quoteLock)
					break;
			}

			// Return string slice
			return (numChars == 0) ? "" : m_String.AsSpan(startIdx, numChars - 1);
		}

		private bool IsCharNumberFast(char c) => c is >= '0' and <= '9';

		public ReadOnlySpan<char> PeekToken(bool supportCommentsAndQuotes = true, bool eatAllBracketContent = false, bool disableNumLock = false)
		{
			int index = m_Index;
			
			try
			{
				return ReadToken(supportCommentsAndQuotes, eatAllBracketContent, disableNumLock);
			}
			finally
			{
				m_Index = index;
			}
		}

		public bool PeekToken(string expected, bool supportCommentsAndQuotes = true, bool eatAllBracketContent = false, bool disableNumLock = false)
		{
			return PeekToken(supportCommentsAndQuotes, eatAllBracketContent, disableNumLock).Equals(expected.AsSpan(), StringComparison.InvariantCultureIgnoreCase);
		}
		
		public bool MatchToken(string expected, bool supportCommentsAndQuotes = true, bool eatAllBracketContent = false, bool disableNumLock = false)
		{
			ReadOnlySpan<char> token = ReadToken(supportCommentsAndQuotes, eatAllBracketContent, disableNumLock);

			bool tokenMatches;
			if (expected.Length == 1)
			{
				tokenMatches = (token[0] == expected[0]);
			}
			else
			{
				tokenMatches = token.Equals(expected.AsSpan(), StringComparison.InvariantCulture);
			}

			if (tokenMatches)
				return true;

			throw new InvalidDataException($"Expected a '{expected}', got a '{token.ToString()}'");
		}
		
		public Vector3 ReadVector3(bool convertToUnity = true)
		{
			MatchToken("(");
			Vector3 vec = new()
			{
				x = ReadFloat(),
				y = ReadFloat(),
				z = ReadFloat()
			};
			MatchToken(")");

			float scale = TrembleSyncSettings.Get().ImportScale;
			return convertToUnity ? vec.Q3ToUnityVector(scale) : vec;
		}

		public float ReadFloat()
		{
			ReadOnlySpan<char> floatValue = ReadToken(supportCommentsAndQuotes: false);
			if (!floatValue.TryParseFloat(out float val))
			{
				Debug.Log($"Not a float '{floatValue.ToString()}'!");
			}

			return val;
		}

		public ulong ReadUlong() => ulong.Parse(ReadToken(supportCommentsAndQuotes: false), NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat);


		public float[] ReadMatrix(int length)
		{
			float[] matrix = new float[length];
			
			MatchToken("[");
			for (int tok = 0; tok < length; tok++)
			{
				matrix[tok] = ReadFloat();
			}
			MatchToken("]");

			return matrix;
		}
	}
}