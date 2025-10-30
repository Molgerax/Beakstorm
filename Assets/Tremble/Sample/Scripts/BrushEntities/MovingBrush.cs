using UnityEngine;

namespace TinyGoose.Tremble.Sample
{
	// A script which references a Point Entity as a target and moves to it.
	// You might use something like this for a moving platform!
	
	[BrushEntity("moving", category: "sample", type: BrushType.Solid)]
	public class MovingBrush : MonoBehaviour, IOnImportFromMapEntity
	{
		// The target position to move to
		[SerializeField] private MovingBrushTarget m_Target;
		
		// The movement speed - using a custom name in Tremble/TrenchBroom for demo purposes
		[SerializeField, Tremble("movement_speed")] private float m_MoveSpeed = 1f;

		[SerializeField] private bool m_ReversedEasing;

		// This is serialised, but TrenchBroom won't show it because it's not a supported type
		[SerializeField] private Rigidbody m_Rigidbody;
		
		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private Vector3 m_StartPosition;
		private Vector3 m_TargetPosition;

		private float m_PositionAlpha = 0f;
		private bool m_Backwards;

		public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
		{
			// Add a rigidbody to our brush and make it kinematic
			m_Rigidbody = gameObject.AddComponent<Rigidbody>();
			m_Rigidbody.isKinematic = true;
		}

		private void Start()
		{
			m_StartPosition = transform.position;
			m_TargetPosition = m_Target ? m_Target.transform.position : m_StartPosition;
		}

		private void Update()
		{
			// Move along line
			m_PositionAlpha += Time.deltaTime * m_MoveSpeed * (m_Backwards ? -1f : +1f);

			// Quick maths to make it smoother
			float visualPositionAlpha = m_ReversedEasing ? 1f - Mathf.Pow(1f - m_PositionAlpha, 3f): Mathf.Pow(m_PositionAlpha, 3f);
			
			Vector3 newPosition = Vector3.Lerp(m_StartPosition, m_TargetPosition, 1f - visualPositionAlpha);
			m_Rigidbody.MovePosition(newPosition);
			transform.position = newPosition;
			
			// Reverse at ends!
			if (m_PositionAlpha < 0f)
			{
				m_Backwards = false;
			}

			if (m_PositionAlpha > 1f)
			{
				m_Backwards = true;
			}
		}
	}
}