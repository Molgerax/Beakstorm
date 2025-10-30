using UnityEngine;

namespace TinyGoose.Tremble.Sample
{
	// Empty point entity to store where moving platforms should move to.
	// Point entities are imported as blank GameObjects with a Transform and the MonoBehaviour component below.
	
	[PointEntity("moving_target", category: "sample")]
	public class MovingBrushTarget : MonoBehaviour
	{
		
	}
}