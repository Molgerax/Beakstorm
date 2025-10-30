using UnityEngine;

namespace TinyGoose.Tremble.Sample
{
	// An invisible trigger box for scoring sheep!

	// We use "colour" to generate a green checkboard texture, which is used in the map.
	
	[BrushEntity("score_trigger", category: "sample", type: BrushType.Trigger,
		colour: "0 1 0", checkerStyle: CheckerboardStyle.Light)]
	public class ScoreTrigger : MonoBehaviour
	{
		// The score trigger doesn't need to do anything - it's just used to mark
		// a volume as a score trigger.
	}
}