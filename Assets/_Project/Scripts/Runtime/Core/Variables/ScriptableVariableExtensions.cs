namespace Beakstorm.Core.Variables
{
    public static class ScriptableVariableExtensions
    {
        //public static float GetValue(this FloatVariable variable) => variable == null ? default : variable.Value;
        public static T TryGet<T>(this ScriptableVariable<T> variable, T alternative = default) => variable == null ? alternative : variable.Value;

        
        public static T Get<T>(this ScriptableVariable<T> variable) => variable == null ? default : variable.Value;

        public static void Set<T>(this ScriptableVariable<T> variable, T value)
        {
            if (variable)
                variable.Value = value;
        }

    }
}
