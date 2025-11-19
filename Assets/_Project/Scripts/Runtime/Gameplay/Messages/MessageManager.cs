using System;
using System.Collections.Generic;
using Beakstorm.Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Beakstorm.Gameplay.Messages
{
    public class MessageManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Image timerBar;
        [SerializeField] private Image skipButton;
        [SerializeField] private GameObject parent;

        private static Queue<Message> _messageQueue = new();

        private static Message CurrentMessage => _messageQueue.Count > 0 ? _messageQueue.Peek() : null;

        private Message _cachedMessage;
        
        public static void AddMessage(Message message)
        {
            _messageQueue.Enqueue(message);
        }
        

        private void OnEnable()
        {
            PlayerInputs.Instance.whistleAction.performed += OnSkipMessage;
            SetMessageUI(null);
        }
        
        private void OnDisable()
        {
            PlayerInputs.Instance.whistleAction.performed -= OnSkipMessage;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void SetMessageUI(Message message)
        {
            if (message == null)
            {
                SetTimer(0);
                SetText(String.Empty);
                SetParent(false);
                SetSkip(false);
            }
            else
            {
                SetText(message.Text);
                SetTimer(message.Timer01);
                SetParent(true);
                SetSkip(message.IsSkippable);
            }
        }

        private void Tick(float dt)
        {
            if(CurrentMessage == null)
                return;

            if (_cachedMessage != CurrentMessage)
            {
                SetMessageUI(CurrentMessage);
                _cachedMessage = CurrentMessage;
            }
            
            CurrentMessage.Tick(dt);
            SetTimer(CurrentMessage.Timer01);
            if (CurrentMessage.Skip)
                Dequeue();
        }

        private void SetText(string messageText)
        {
            if (text)
                text.text = messageText;
        }
        
        private void SetTimer(float value01)
        {
            if (timerBar)
                timerBar.fillAmount = value01;
        }

        private void SetParent(bool active)
        {
            if (parent)
                parent.SetActive(active);
        }

        private void SetSkip(bool active)
        {
            if (skipButton)
                skipButton.enabled = active;
        }

        private void Dequeue()
        {
            if (_messageQueue.Count == 0)
                return;

            _messageQueue.Dequeue();
            SetMessageUI(CurrentMessage);
        }
        
        private void OnSkipMessage(InputAction.CallbackContext context)
        {
            if (CurrentMessage is {IsSkippable: true})
                Dequeue();
        }
    }

    public class Message
    {
        public readonly string Text;

        public float Timer;
        public readonly bool IsSkippable;

        private readonly float _maxTime;
        
        public float Timer01 => _maxTime > 0 && !IsSkippable ? Timer / _maxTime : 0;

        public bool Skip => !IsSkippable && Timer01 == 0;
        

        public Message(string text, bool isSkippable = true, float time = 10)
        {
            Text = text;
            Timer = time;
            _maxTime = time;
            IsSkippable = isSkippable;
        }

        public void Tick(float dt)
        {
            if (IsSkippable)
                return;
            
            Timer = Mathf.MoveTowards(Timer, 0, dt);
        }
    }
}
