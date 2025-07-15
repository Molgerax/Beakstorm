using UnityEngine;

namespace Beakstorm
{
    public class SoundManager : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

            //Section for SFX One-Shots
            //AkSoundEngine.PostEvent("EventName", this.gameObject);

            //Bird Target, Plays When Pheromone Dart hits target

            AkSoundEngine.PostEvent("play_birdTarget", this.gameObject);

            //Bird Attack, Plays When Weak Point Takes Damage

            AkSoundEngine.PostEvent("play_birdAttack", this.gameObject);

            //Bird Static, Plays When Player is Close to a Flock

            AkSoundEngine.PostEvent("play_birdStatic", this.gameObject);

            //Phero Drag, Plays When Player is Dispensing Pheromones Behind

            AkSoundEngine.PostEvent("play_pheroDrag", this.gameObject);

            //Phero Shoot, Plays When Player Shoots Pheromone Dart

            AkSoundEngine.PostEvent("play_pheroShoot", this.gameObject);

            //Ship Cannon, Plays When Big Ship Cannon Shoots

            AkSoundEngine.PostEvent("play_shipCannon", this.gameObject);

            //Ship Turret, Plays When Small Shup Turret Shoots

            AkSoundEngine.PostEvent("play_birdStatic", this.gameObject);



            //Section for Dynamic Music
            //AkSoundEngine.SetState(string stateGroup, string stateValue);
            //(for 1.0) set wave_Type to war  Each Time a new Wave is spawned, set wave_State to war1 -> war 2 -> war...


        }
    }
}
