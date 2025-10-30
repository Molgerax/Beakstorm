using System;
using UnityEngine;

namespace TinyGoose.Tremble
{
	   [Serializable]
       public class MaterialRenderData
       {
   	    public MaterialRenderData(Material material)
   	    {
   		    m_Material = material;
   	    }

   	    [SerializeField] private Material m_Material;
   	    [SerializeField] private MaterialImportMode m_ExportMode;
   	    [SerializeField] private bool m_IsResolutionOverridden;
   	    [SerializeField] private MaterialExportSize m_ResolutionX = MaterialExportSize.Size64;
   	    [SerializeField] private MaterialExportSize m_ResolutionY = MaterialExportSize.Size64;
   	    [SerializeField] private bool m_Dirty;

   	    public Material Material => m_Material;

   	    public MaterialImportMode ExportMode
   	    {
   		    get => m_ExportMode;
   		    set
   		    {
   			    m_ExportMode = value;
   			    m_Dirty = true;
   		    }
   	    }

   	    public bool IsResolutionOverridden
   	    {
   		    get => m_IsResolutionOverridden;
   		    set
   		    {
   			    m_IsResolutionOverridden = value;
   			    m_Dirty = true;
   		    }
   	    }

   	    public MaterialExportSize ResolutionX
   	    {
   		    get => m_ResolutionX;
   		    set
   		    {
   			    m_ResolutionX = value;
   			    m_Dirty = true;
   		    }
   	    }

   	    public MaterialExportSize ResolutionY
   	    {
   		    get => m_ResolutionY;
   		    set
   		    {
   			    m_ResolutionY = value;
   			    m_Dirty = true;
   		    }
   	    }

   	    public Vector2Int GetResolutionOrDefault()
   	    {
   		    if (!m_IsResolutionOverridden)
   			    return TrembleSyncSettings.Get().MaterialExportSize.ToVector2IntSize();

   		    return new(m_ResolutionX.ToIntSize(), m_ResolutionY.ToIntSize());
   	    }

   	    public bool IsDirty => m_Dirty;
   	    public void ClearDirty() => m_Dirty = false;
       }


       public enum MaterialImportMode
       {
   	    MainTex,
   	    Legacy,
       }
}