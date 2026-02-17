using Beakstorm.Gameplay.Messages;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping
{
    public abstract class TriggerSender : MonoBehaviour, IOnImportFromMapEntity
    {
        [SerializeField, NoTremble] protected Component[] targets;
        
        [Tremble("target")] private ITriggerTarget[] _targets;

        [SerializeField, NoTremble] protected bool invertSignal;
        [Tremble, SpawnFlags(8)] private bool _invertSignal;

        [SerializeField, NoTremble] protected float delayTime;
        [SerializeField, NoTremble] private string msg;

        public void SendTrigger(TriggerData data = default)
        {
            if (invertSignal)
                data.Activate = !data.Activate;

            if (delayTime > 0)
            {
                DelayedTriggerCall(data, delayTime);
            }
            else
            {
                RawTrigger(data);
            }
        }

        private async void DelayedTriggerCall(TriggerData data, float delay)
        {
            float timer = delay;
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                await Awaitable.NextFrameAsync(destroyCancellationToken);
            }
            RawTrigger(data);
        }

        
        private void RawTrigger(TriggerData data)
        {
            targets.TryTrigger(data);
            
            if (!msg.IsNullOrEmpty())
                MessageManager.AddMessage(new(msg, false, 5f));
        }
        
        
        public virtual void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            targets = _targets.TriggerToComponent();
            invertSignal = _invertSignal;

            if (entity.TryGetFloat("delay", out float delay))
                delayTime = delay;
            else
                delayTime = 0;

            msg = entity.TryGetString("message", out string m) ? m : null;
        }


        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = new(0, 1, 0, 0.5f);

            foreach (Component target in targets)
            {
                DrawGizmoArrow(transform.position, target.transform.position);
            }
        }
        
        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = new(1, 0, 0);

            foreach (Component target in targets)
            {
                DrawGizmoArrow(transform.position, target.transform.position);
            }
        }

        protected void DrawGizmoArrow(Vector3 startPos, Vector3 endPos)
        {
            Gizmos.DrawLine(startPos, endPos);
            
            float percentage = 0.75f;
            Vector3 diff = endPos - startPos;
            Vector3 arrowBase = startPos + diff * percentage;

            
            DrawGizmoArrowHead(arrowBase, diff.normalized);
        }

        protected void DrawGizmoArrowHead(Vector3 pos, Vector3 dir)
        {
            int count = 3;
            float length = 3f;

            Vector3 arrowDirection = -dir.normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir).normalized;
            arrowDirection = Quaternion.AngleAxis(22.5f, right) * arrowDirection;

            for (int i = 0; i < count; i++)
            {
                float degrees = 360f / count;
                Gizmos.DrawLine(pos, pos + arrowDirection * length);
                arrowDirection = Quaternion.AngleAxis(degrees, dir.normalized) * arrowDirection;
            }
        }
    }
}