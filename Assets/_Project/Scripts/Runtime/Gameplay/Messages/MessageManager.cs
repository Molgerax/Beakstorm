using System;
using System.Collections.Generic;
using Beakstorm.Inputs;
using Beakstorm.Mapping;
using Beakstorm.UI.Icons;
using Beakstorm.Utility.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PrimeTween;

namespace Beakstorm.Gameplay.Messages
{
    public class MessageManager : MonoBehaviour
    {
        [SerializeField] private ButtonIcons icons;
        [SerializeField] private TMP_Text text;
        [SerializeField] private Image timerBar;
        [SerializeField] private Image skipButton;
        [SerializeField] private GameObject parent;

        private static Queue<Message> _messageQueue = new();

        private static Message CurrentMessage => _messageQueue.Count > 0 ? _messageQueue.Peek() : null;

        private Message _cachedMessage;

        private RectTransform _rect;
        
        private string _cachedText;
        private bool _disabled;

        private bool Disabled
        {
            get => _disabled;
            set
            {
                if (_disabled == value)
                    return;

                if (value)
                    DeactivateMessageWindow();
                else
                    ActivateMessageWindow();
                _disabled = value;
            }
        }

        private void ActivateMessageWindow()
        {
            if (!_rect)
                return;

            _tween.Complete();
            SetParent(true);
            _tween = Tween.UIAnchoredPosition(_rect, Vector2.up * 224, Vector2.zero, 0.2f,
                Easing.Standard(Ease.InOutQuad));
        }
        
        private void DeactivateMessageWindow()
        {
            if (!_rect)
                return;

            _tween.Complete();
            _tween = Tween.UIAnchoredPosition(_rect, Vector2.zero, Vector2.up * 224, 0.2f,
                Easing.Standard(Ease.InOutQuad)).OnComplete(this, (x) =>
            {
                x.SetParent(false);
            });
        }
        
        private Tween _tween;
        
        public static void AddMessage(Message message)
        {
            _messageQueue.Enqueue(message);
        }
        

        private void OnEnable()
        {
            PlayerInputs.Instance.Whistle += OnSkipMessage;
            PlayerInputs.ActiveDeviceChangeEvent += OnDeviceChanged;
            OnDeviceChanged();
            SetMessageUI(null);
            SetParent(false);

            if (parent)
                _rect = (RectTransform) parent.transform;
        }
        
        private void OnDisable()
        {
            PlayerInputs.Instance.Whistle -= OnSkipMessage;
            PlayerInputs.ActiveDeviceChangeEvent -= OnDeviceChanged;
            
            _messageQueue.Clear();
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
                SetSkip(false);
                Disabled = true;
            }
            else
            {
                SetText(message.Text);
                SetTimer(message.Timer01);
                SetParent(true);
                SetSkip(message.IsSkippable);
                Disabled = false;
            }
        }

        private void Tick(float dt)
        {
            if (CurrentMessage == null)
            { 
                if (!Disabled)
                    SetMessageUI(null);
                return;
            }

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

        private void OnDeviceChanged()
        {
            if (!string.IsNullOrEmpty(_cachedText))
                text.text =
                    CompleteTextWithButtonPromptSprite.ReplaceActiveBindings(_cachedText, PlayerInputs.Instance, icons);
            text.spriteAsset = icons.GetAssetByDevice(PlayerInputs.LastActiveDevice);
        }

        private void SetText(string messageText)
        {
            _cachedText = messageText;
            if (text)
                text.text =
                    CompleteTextWithButtonPromptSprite.ReplaceActiveBindings(_cachedText, PlayerInputs.Instance, icons);
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
            CurrentMessage?.Finish();
            
            if (_messageQueue.Count == 0)
                return;

            _messageQueue.Dequeue();
            SetMessageUI(CurrentMessage);
        }
        
        private void OnSkipMessage(bool performed)
        {
            if (!performed)
                return;
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

        private readonly List<TriggerBehaviour> _targets;
        
        public float Timer01 => _maxTime > 0 && !IsSkippable ? Timer / _maxTime : 0;

        public bool Skip => !IsSkippable && Timer01 == 0;
        

        public Message(string text, bool isSkippable = true, float time = 10, List<TriggerBehaviour> targets = null)
        {
            Text = text;
            Timer = time;
            _maxTime = time;
            IsSkippable = isSkippable;
            _targets = targets;
        }

        public void Tick(float dt)
        {
            if (IsSkippable)
                return;
            
            Timer = Mathf.MoveTowards(Timer, 0, dt);
        }

        public void Finish()
        {
            _targets.TryTrigger();
        }
    }
}
