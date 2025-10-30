using UnityEngine;

namespace TinyGoose.Tremble.Sample
{
	// A sample ScriptableObject for our player. In our case, only holds movement and turn speed 
	// settings.
	// This is mostly just to show that Tremble supports ScriptableObjects out of the box ;)
	[CreateAssetMenu(menuName = "Tremble Sample/Player Data")]
	public class PlayerData : ScriptableObject
	{
		[SerializeField] private float m_MovementSpeed = 100f;
		[SerializeField] private float m_TurnSpeed = 1000f;
		[SerializeField] private float m_AirControl = 0.1f;
		[SerializeField, Range(0f, 1f)] private float m_Braking = 0.9f;
		[SerializeField] private float m_JumpForce = 10f;
		
		public float MovementSpeed => m_MovementSpeed;
		public float TurnSpeed => m_TurnSpeed;
		public float AirControl => m_AirControl;
		public float Braking => m_Braking;
		public float JumpForce => m_JumpForce;
	}
}