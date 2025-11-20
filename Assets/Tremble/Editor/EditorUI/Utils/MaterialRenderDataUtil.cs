using System;
using UnityEditor;
using UnityEngine;

namespace TinyGoose.Tremble.Editor
{
    public static class MaterialRenderDataUtil
    {
        public static void SetMaterialImportMode(this Material mat, MaterialImportMode mode)
            => ModifyRenderData(mat, mrd => mrd.ExportMode = mode);

        public static void SetMaterialResolutionOverridden(this Material mat, bool overrideResolution)
            => ModifyRenderData(mat, mrd => mrd.IsResolutionOverridden = overrideResolution);

        public static void SetMaterialResolutionX(this Material mat, MaterialExportSize size)
            => ModifyRenderData(mat, mrd => mrd.ResolutionX = size);

        public static void SetMaterialResolutionY(this Material mat, MaterialExportSize size)
            => ModifyRenderData(mat, mrd => mrd.ResolutionY = size);


        private static void ModifyRenderData(Material mat, Action<MaterialRenderData> action)
        {
            TrembleSyncSettings syncSettings = TrembleSyncSettings.Get();
            MaterialRenderData renderData = TrembleSyncSettings.Get().GetMaterialRenderData(mat);

            action(renderData);
            EditorUtility.SetDirty(syncSettings);
        }
    }
}