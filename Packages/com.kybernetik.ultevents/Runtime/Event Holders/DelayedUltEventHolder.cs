﻿// UltEvents // https://kybernetik.com.au/ultevents // Copyright 2021-2024 Kybernetik //

using System.Collections;
using UnityEngine;

namespace UltEvents
{
    /// <summary>
    /// A component which encapsulates a single <see cref="UltEventBase"/> with a delay before its invocation.
    /// </summary>
    [AddComponentMenu(UltEventUtils.ComponentMenuPrefix + "Delayed Ult Event Holder")]
    [UltEventsHelpUrl(typeof(DelayedUltEventHolder))]
    public class DelayedUltEventHolder : UltEventHolder
    {
        /************************************************************************************************************************/

        [SerializeField]
        private float _Delay;

        private WaitForSeconds _Wait;

        /************************************************************************************************************************/

        /// <summary>
        /// The number of seconds that will pass between calling <see cref="Invoke"/> and the event actually being invoked.
        /// </summary>
        public float Delay
        {
            get => _Delay;
            set
            {
                _Delay = value;
                _Wait = null;
            }
        }

        /************************************************************************************************************************/

        /// <summary>Ensures that the wait time isn't improperly cached.</summary>
        protected virtual void OnValidate()
        {
            _Wait = null;
        }

        /************************************************************************************************************************/

        /// <summary>Waits for <see cref="Delay"/> seconds then calls Event.Invoke().</summary>
        public override void Invoke()
        {
            if (_Delay < 0)
                base.Invoke();
            else
                StartCoroutine(DelayedInvoke());
        }

        /************************************************************************************************************************/

        private IEnumerator DelayedInvoke()
        {
            _Wait ??= new(_Delay);

            yield return _Wait;

            base.Invoke();
        }

        /************************************************************************************************************************/
    }
}