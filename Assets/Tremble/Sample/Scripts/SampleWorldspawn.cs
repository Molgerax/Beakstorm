using UnityEngine;

namespace TinyGoose.Tremble.Sample
{
	public class SampleWorldspawn : Worldspawn
	{
		[SerializeField] private string m_EntryMessage;
		[SerializeField] private int m_MaxSheep;

		public string EntryMessage => m_EntryMessage;
		public int MaxSheep => m_MaxSheep;
	}
}