using UnityEngine;

namespace TinyGoose.Tremble.Sample
{
	// Not serialisable in maps! A sheep.
	public class Sheep : MonoBehaviour
	{
		[SerializeField] private Rigidbody m_Rigidbody;

		[SerializeField] private Material m_MatSheepEyeClosed;
		[SerializeField] private Material m_MatSheepEyeOpen;

		[SerializeField] private MeshRenderer[] m_Eyes;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private Player m_Player;
		private SheepTarget m_Target;

		private float m_BlinkOffset;
		private bool m_WasCaptured;

		// This is to workaround Rigidbody::velocity being renamed to Rigidbody::linearVelocity in Unity 6+.
		// For your own projects, just use whichever one is appropriate - I just need this sample to work for all ;)
#if UNITY_6000_0_OR_NEWER
		private Vector3 Velocity { get => m_Rigidbody.linearVelocity;set => m_Rigidbody.linearVelocity = value; }
#else
		private Vector3 Velocity { get => m_Rigidbody.velocity; set => m_Rigidbody.velocity = value; }
#endif
		
		private void Start()
		{
			// Spin on spawn so we don't just stack ;)
			m_Rigidbody.angularVelocity = Random.onUnitSphere * 5f;
			
			// Find player and a target in the map (not ideal, but good for sample)
			MapDocument map = gameObject.GetComponentInParent<MapDocument>();
			m_Player = map.GetComponentInChildren<Player>();
			m_Target = map.GetComponentInChildren<SheepTarget>();

			m_BlinkOffset = Random.Range(0f, 1f);
		}

		private void Update()
		{
			// Goofy captured effect
			if (m_WasCaptured)
			{
				float currentScale = transform.localScale.x;
				transform.localScale = Vector3.one * Mathf.Lerp(currentScale, 0f, Time.deltaTime * 2f);
			}

			// Blink!
			float blinkTime = (Time.time + m_BlinkOffset) % 3f;
			foreach (MeshRenderer eye in m_Eyes)
			{
				eye.material = blinkTime < 2.9f ? m_MatSheepEyeOpen : m_MatSheepEyeClosed;
			}
		}

		private void FixedUpdate()
		{
			if (m_WasCaptured)
			{
				Vector3 velocity = Velocity;
				float speed = velocity.magnitude;

				Velocity = velocity.normalized * Mathf.Lerp(speed, 0f, Time.deltaTime * 2f);
				return;
			}
			
			Vector3 myPos = transform.position;
			Vector3 playerPos = m_Player.transform.position;

			Vector3 awayFromPlayer = myPos - playerPos;
			float repulsion = 30f - Mathf.Clamp(awayFromPlayer.magnitude, 0f, 30f);

			if (repulsion > 0.01f)
			{
				// Move away from player
				m_Rigidbody.AddForce(awayFromPlayer.normalized * (repulsion * 0.75f), ForceMode.Acceleration);
				
				// Now also move towards target a bit ;)
				if (m_Target)
				{
					Vector3 toTarget = (m_Target.transform.position - myPos).normalized;
					m_Rigidbody.AddForce(toTarget * (repulsion * 2f), ForceMode.Acceleration);
				}
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			if (m_WasCaptured || !other.TryGetComponent(out ScoreTrigger st))
				return;

			m_WasCaptured = true;

			// Find the map's SceneController, and add our score to it
			MapDocument map = gameObject.GetComponentInParent<MapDocument>();
			SceneController sceneController = map.GetComponentInChildren<SceneController>();
			sceneController.AddScore(10);

			Destroy(gameObject, 5f);
		}
	}
}
