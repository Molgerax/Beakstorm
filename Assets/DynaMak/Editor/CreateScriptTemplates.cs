using UnityEditor;

namespace DynaMak.Editors
{
    public static class CreateScriptTemplates
    {
        [MenuItem("Assets/Create/DynaMak/Particle Compute Shader")]
        public static void CreateTemplateMenuItem()
        {
            string templatePath = "Assets/DynaMak/Editor/Templates/DynaMak-ParticleCompute.compute.txt";
            
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templatePath, "NewParticleSystem.compute");
        }
    }
}
