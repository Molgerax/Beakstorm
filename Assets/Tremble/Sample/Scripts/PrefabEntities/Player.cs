using UnityEngine;

namespace TinyGoose.Tremble.Sample
{
	// The player. Spawnable as a prefab in the map editor, where you can also change the move/turn speeds.
	[PrefabEntity(category: "sampleprefab")]
	public class Player : MonoBehaviour
	{
		// -----------------------------------------------------------------------------------------------------------------------------
		//		Serialized
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField] private PlayerData m_PlayerData; // This can be changed in map editor!

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Components
		// -----------------------------------------------------------------------------------------------------------------------------
		[SerializeField] private Rigidbody m_Rigidbody;
		[SerializeField] private Camera m_Camera;

		// -----------------------------------------------------------------------------------------------------------------------------
		//		Getters
		// -----------------------------------------------------------------------------------------------------------------------------
		// This is to workaround Rigidbody::velocity being renamed to Rigidbody::linearVelocity in Unity 6+.
		// For your own projects, just use whichever one is appropriate - I just need this sample to work for all ;)
#if UNITY_6000_0_OR_NEWER
		private Vector3 Velocity { get => m_Rigidbody.linearVelocity;set => m_Rigidbody.linearVelocity = value; }
#else
		private Vector3 Velocity { get => m_Rigidbody.velocity; set => m_Rigidbody.velocity = value; }
#endif

		// -----------------------------------------------------------------------------------------------------------------------------
		//		State
		// -----------------------------------------------------------------------------------------------------------------------------
		private float m_CameraPitch;
		private bool m_IsGrounded;

		private void OnEnable()
		{
			Cursor.lockState = CursorLockMode.Locked;
		}

		private void OnDisable()
		{
			Cursor.lockState = CursorLockMode.None;
		}

		private void Update()
		{
			// Look based on mouse input
			Look();

			// Jump if key pressed
			if (Input.GetButtonDown("Jump"))
			{
				Jump();
			}

			// Handle cursor
			if (Input.GetButtonDown("Fire1"))
			{
				Cursor.lockState = CursorLockMode.Locked;
			}
			else if (Input.GetKeyDown(KeyCode.Escape))
			{
				Cursor.lockState = CursorLockMode.None;
			}
		}

		private void FixedUpdate()
		{
			m_IsGrounded = Physics.Raycast(transform.position, Vector3.down, 2.5f);

			FixedMovement();
		}

		private void FixedMovement()
		{
			// Move in input direction. Bad code - don't copy me ;)
			Vector3 movement = new()
			{
				x = Input.GetAxisRaw("Horizontal"),
				y = 0f,
				z = Input.GetAxisRaw("Vertical")
			};
			movement = transform.rotation * movement;

			Vector3 flatVelocity = Velocity;
			flatVelocity.y = 0f;

			// Moving/braking control
			float movementScalar = m_IsGrounded ? 1f : m_PlayerData.AirControl;
			if (movement.sqrMagnitude > 0.5f * 0.5f)
			{
				// Move
				m_Rigidbody.AddForce(movement * (m_PlayerData.MovementSpeed * movementScalar), ForceMode.Acceleration);
			}
			else
			{
				// Brake
				m_Rigidbody.AddForce(-flatVelocity * m_PlayerData.Braking, ForceMode.Acceleration);
			}

			// Clamp top speed
			if (flatVelocity.sqrMagnitude > 10f * 10f)
			{
				Velocity = flatVelocity.normalized * 10f + new Vector3(0f, Velocity.y, 0f);
			}

			// Extra gravity
			if (!m_IsGrounded)
			{
				Velocity += Vector3.down * (m_PlayerData.JumpForce * 0.02f);
			}
		}

		private const float TURN_SPEED_MULTIPLIER =
#if UNITY_EDITOR
			2f;
#else
			1f;
#endif

		private void Look()
		{
			Vector2 mouseMovement = new()
			{
				x = Input.GetAxis("Mouse X"),
				y = Input.GetAxis("Mouse Y")
			};

			transform.Rotate(Vector3.up, mouseMovement.x * Time.deltaTime * m_PlayerData.TurnSpeed * TURN_SPEED_MULTIPLIER);

			m_CameraPitch -= mouseMovement.y * Time.deltaTime * m_PlayerData.TurnSpeed * TURN_SPEED_MULTIPLIER;
			m_CameraPitch = Mathf.Clamp(m_CameraPitch, -40f, +40f);

			m_Camera.transform.localRotation = Quaternion.Euler(m_CameraPitch, 0f, 0f);
		}

		private void Jump()
		{
			if (!m_IsGrounded)
				return;

			Velocity += Vector3.up * m_PlayerData.JumpForce;
		}
	}
}