using UnityEngine;
using UnityEngine.UI;

namespace TinyGoose.Tremble.Sample
{
	public class SceneController : MonoBehaviour
	{
		// These cannot be serialised in the map file, but the values from the prefab are kept.
		[SerializeField] private Text m_WelcomeMessageText;
		[SerializeField] private Text m_ScoreText;

		// Simple (but terrible) singleton - just for the sample
		private static SceneController s_SceneController;
		public static SceneController Get() => s_SceneController;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private int m_Score;

		private void Awake()
		{
			// We are the singleton now!
			s_SceneController = this;
		}

		private void Start()
		{
			// Find the map and get its worldspawn
			MapDocument map = gameObject.GetComponentInParent<MapDocument>();
			SampleWorldspawn sampleWorldspawn = map.GetWorldspawn<SampleWorldspawn>();

			if (!sampleWorldspawn)
			{
				Debug.LogError("Sample: Change Tremble Settings > Basic Settings > Worldspawn Script to 'Sample Worldspawn' and reimport the map to enable all functionality!");
				return;
			}

			// Set the welcome text from the map
			m_WelcomeMessageText.text = sampleWorldspawn.EntryMessage;
		}

		public void AddScore(int score)
		{
			m_Score += score;
			m_ScoreText.text = "Score: " + m_Score;
		}
	}
}