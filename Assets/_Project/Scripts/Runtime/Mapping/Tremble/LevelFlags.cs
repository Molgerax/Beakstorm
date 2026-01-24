using Beakstorm.Mapping.Tremble.Properties;
using UnityEngine;

namespace Beakstorm.Mapping.Tremble
{
    public class LevelFlags : MonoBehaviour
    {
        public static LevelFlags Current;

        [SerializeField] private QuakeFlags flags;

        #region Mono Methods

        private void OnEnable()
        {
            Current = this;
        }

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }
        
        #endregion


        #region Public Methods

        public static void SetFlag(int bit, bool value)
        {
            if (Current)
                Current.flags.SetFlag(bit, value);
        }
        
        public static bool GetFlag(int bit, bool value)
        {
            if (Current)
                return Current.flags.GetFlag(bit);
            return false;
        }

        #endregion
    }
}