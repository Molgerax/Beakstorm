using UnityEngine;

namespace DynaMak.Volumes
{
    public abstract class VolumeComponent : MonoBehaviour
    {
        public abstract VolumeTexture GetVolumeTexture();

        public virtual Vector3 VolumeCenter => GetVolumeTexture() is null ? Vector3.zero : GetVolumeTexture().Center;
        public virtual Vector3 VolumeBounds => GetVolumeTexture() is null ? Vector3.zero : GetVolumeTexture().Bounds;
        public virtual Vector3Int VolumeResolution => GetVolumeTexture() is null ? Vector3Int.zero : GetVolumeTexture().Resolution;
    }
}