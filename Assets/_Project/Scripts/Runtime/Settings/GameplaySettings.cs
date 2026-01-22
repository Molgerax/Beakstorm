using UnityEngine;

namespace Beakstorm.Settings
{
    [CreateAssetMenu(menuName = "Beakstorm/Settings/GameplaySettings", fileName = "NewGameplaySettings")]
    public class GameplaySettings : AbstractSettingsData<GameplaySettings>
    {
        public override string FileName => "GameplaySettings";


        public bool InvertLookAxisX = false;
        public bool InvertLookAxisY = false;
        
        public bool InvertFlightAxisX = false;
        public bool InvertFlightAxisY = false;

        public float MouseSensitivity = 5f;

        public bool UseMouseAsMoveInput => false;
        
        public void SetInvertLookAxisX(bool value) => InvertLookAxisX = value; 
        public void SetInvertLookAxisY(bool value) => InvertLookAxisY = value; 
        public void SetInvertFlightAxisX(bool value) => InvertFlightAxisX = value; 
        public void SetInvertFlightAxisY(bool value) => InvertFlightAxisY = value; 
        public void SetMouseSensitivity(float value) => MouseSensitivity = value;

        public Vector2 LookAxisInversion => new(InvertLookAxisX ? -1 : 1, InvertLookAxisY ? -1 : 1);
        public Vector2 FlightAxisInversion => new(InvertFlightAxisX ? -1 : 1, InvertFlightAxisY ? -1 : 1);
    }
}
