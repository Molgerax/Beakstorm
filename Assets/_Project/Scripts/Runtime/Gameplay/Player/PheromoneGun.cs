using System;
using Beakstorm.Gameplay.Player.Weapons;
using Beakstorm.Gameplay.Targeting;
using Beakstorm.Inputs;
using UltEvents;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    public class PheromoneGun : MonoBehaviour
    {
        [SerializeField, Range(0, 5f)] private float shootOffset = 3f;
        [SerializeField] private LayerMask layerMask;

        [SerializeField] private PheromoneWeaponInventory weaponInventory;
        
        [SerializeField] private TargetingManager targetingManager;
        
        [SerializeField] private UltEvent onShoot;
        
        private PlayerInputs _inputs;
        private Camera _camera;

        private void Awake()
        {
            _inputs = PlayerInputs.Instance;
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            _inputs.Shoot += OnShootActionPerformed;
        }

        private void OnDisable()
        {
            
            _inputs.Shoot -= OnShootActionPerformed;
        }

        
        private void OnShootActionPerformed(bool performed)
        {
            if (!performed)
                return;
            
            if (!weaponInventory || weaponInventory.SelectedWeapon == null)
                return;

            Vector3 pos = transform.position;
            Vector3 lookDir = GetShootDirection(pos);

            Vector3 dir = transform.forward;

            Vector3 targetPos = GetTargetPos(out var targetNormal, out var coll, out var target);
            
            pos += dir * shootOffset;

            FireInfo fireInfo = new FireInfo(pos, dir, lookDir, targetPos, targetNormal, 0, coll, target);
            
            if (weaponInventory.SelectedWeapon.TryFire(fireInfo))
                onShoot?.Invoke();
        }

        private Vector3 GetShootDirection(Vector3 shootPosition)
        {
            if (!_camera) return transform.forward;

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            if (targetingManager)
            {
                if (targetingManager.CurrentTarget)
                {
                    Target t = targetingManager.CurrentTarget;
                    ray.direction = t.transform.position - targetingManager.ViewAnchor.position;
                    ray.origin = targetingManager.ViewAnchor.position;

                    return (t.Position - shootPosition).normalized;
                }
            }
            
            if (Physics.Raycast(ray, out RaycastHit hit, Single.MaxValue, layerMask, QueryTriggerInteraction.Ignore))
            {
                return (hit.point - shootPosition).normalized;
            }

            return _camera.transform.forward;
        }
        
        private Vector3 GetTargetPos(out Vector3 targetNormal, out Collider collider, out Target target)
        {
            targetNormal = -transform.forward;
            collider = null;
            target = null;
            if (!_camera) return transform.position + transform.forward * 500;

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            
            if (targetingManager)
            {
                if (targetingManager.CurrentTarget)
                {
                    Target t = targetingManager.CurrentTarget;
                    ray.direction = t.transform.position - targetingManager.ViewAnchor.position;
                    ray.origin = targetingManager.ViewAnchor.position;
                    target = t;
                    return t.Position;
                }
            }

            Vector3 shotPosition = ray.origin + ray.direction * 500f;
            targetNormal = -ray.direction;
            
            if (Physics.Raycast(ray, out RaycastHit hit, Single.MaxValue, layerMask, QueryTriggerInteraction.Ignore))
            {
                targetNormal = hit.normal;
                collider = hit.collider;
                return hit.point;
            }

            return shotPosition;
        }
    }

    public struct FireInfo
    {
        public Vector3 InitialPosition;
        public Vector3 InitialDirection;
        public Vector3 LookDirection;
        private Vector3 _targetPosition;
        private Vector3 _targetNormal;
        public float Speed;
        public Collider Collider;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public Target Target;

        public Vector3 TargetPosition
        {
            get
            {
                if (Target) return Target.Position;
                
                return (Collider && Collider.gameObject.activeInHierarchy)
                    ? Collider.transform.TransformPoint(HitPoint)
                    : _targetPosition;
            }
        }

        public Vector3 TargetNormal => Collider && Collider.gameObject.activeInHierarchy ? Collider.transform.TransformDirection(HitNormal) : _targetNormal;
        
        public FireInfo(Vector3 position, Vector3 direction, Vector3 lookDirection, Vector3 targetPosition, Vector3 targetNormal, float speed, Collider collider = null, Target target = null)
        {
            InitialPosition = position;
            InitialDirection = direction;
            LookDirection = lookDirection;
            _targetPosition = targetPosition;
            _targetNormal = targetNormal;
            Speed = speed;
            Collider = collider;
            HitPoint = targetPosition;
            HitNormal = targetNormal;
            Target = target;

            if (Collider)
            {
                HitPoint = Collider.transform.InverseTransformPoint(_targetPosition);
                HitNormal = Collider.transform.InverseTransformDirection(_targetNormal);
            }
        }
    }
}