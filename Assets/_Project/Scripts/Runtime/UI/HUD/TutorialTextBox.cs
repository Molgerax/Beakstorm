using UltEvents;
using UnityEngine;

namespace Beakstorm.UI.HUD
{
    public class TutorialTextBox : MonoBehaviour
    {
        [SerializeField] private GameObject[] textBoxes;
        [SerializeField] private UltEvent onFinish;
        
        private int _textIndex = 0;
        
        private void OnEnable()
        {
            foreach (GameObject textBox in textBoxes)
            {
                textBox.SetActive(false);;
            }

            _textIndex = -1;
        }

        public void SetProgress(int i)
        {
            GameObject t;

            if (_textIndex >= 0 && _textIndex < textBoxes.Length)
            {
                t = textBoxes[_textIndex];
                if (t)
                    t.SetActive(false);
            }

            _textIndex = i;

            if (_textIndex >= textBoxes.Length)
            {
                onFinish?.Invoke();
                return;
            }
            
            if (_textIndex >= 0 && _textIndex < textBoxes.Length)
            {
                t = textBoxes[_textIndex];
                if (t)
                    t.SetActive(true);
            }
        }

        public void AdvanceText()
        {
            SetProgress(_textIndex+1);
        }
    }
}
