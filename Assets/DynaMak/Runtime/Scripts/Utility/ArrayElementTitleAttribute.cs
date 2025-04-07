// Source from this thread:
// https://forum.unity.com/threads/how-to-change-the-name-of-list-elements-in-the-inspector.448910/

using UnityEngine;

namespace DynaMak.Utility
{
    public class ArrayElementTitleAttribute : PropertyAttribute
    {
        public string VarName;

        public ArrayElementTitleAttribute(string elementTitleVariable = "")
        {
            VarName = elementTitleVariable;
        }
    }
    
    public interface IArrayElementTitle
    {
        public string Name
        {
            get;
        }
    }
}