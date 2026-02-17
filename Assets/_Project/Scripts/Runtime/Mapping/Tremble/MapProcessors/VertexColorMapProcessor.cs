using System.Collections.Generic;
using System.Linq;
using Beakstorm.Mapping.PointEntities.Lights;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.Tremble.MapProcessors
{
    public class VertexColorMapProcessor : MapProcessorBase
    {
        private List<GameObject> _filteredBrushes = new();
        
        public override void ProcessBrushEntity(MapBsp mapBsp, BspEntity entity, GameObject brush)
        {
            base.ProcessBrushEntity(mapBsp, entity, brush);
            if (entity.TryGetInt("_vertexcolor", out int value))
            {
                if (value > 0)
                    _filteredBrushes.Add(brush);
            }
        }

        public override void OnProcessingCompleted(GameObject root, MapBsp mapBsp)
        {
            MapWorldSpawn worldSpawn = GetWorldspawn<MapWorldSpawn>();

            var vertexColors = root.GetComponentsInChildren<LightVertexColor>();
            if (vertexColors == null || vertexColors.Length == 0)
                return;
            
            List<MeshRenderer> meshRenderers = worldSpawn.GetComponentsInChildren<MeshRenderer>().ToList();

            foreach (GameObject brush in _filteredBrushes)
            {
                if (!brush)
                    continue;

                if (brush.TryGetComponent(out MeshRenderer meshRenderer) && brush.TryGetComponent(out MeshFilter filter))
                {
                    meshRenderers.Add(meshRenderer);
                }
            }


            foreach (MeshRenderer meshRenderer in meshRenderers)
            {
                MeshFilter filter = meshRenderer.GetComponent<MeshFilter>();
                Mesh mesh = filter.sharedMesh;

                List<Color> colors = new();
                List<Vector3> vertices = new();
                mesh.GetVertices(vertices);
                foreach (Vector3 vertex in vertices)
                {
                    Vector3 pos = filter.transform.TransformPoint(vertex);
                    float sum = 0;
                    Vector4 sumColor = Vector4.zero;

                    foreach (LightVertexColor vertexColor in vertexColors)
                    {
                        float distance = Vector3.Distance(vertexColor.transform.position, pos);

                        distance *= mapBsp.ImportScale;
                        
                        float influence = Influence(distance, vertexColor.Radius);
                        Color c = vertexColor.Color;
                        sumColor += new Vector4(c.r, c.g, c.b, c.a) * influence;
                        sum += influence;
                    }

                    if (sum == 0)
                        sumColor = Vector4.one;
                    else
                        sumColor /= sum;

                    Color col = new(sumColor.x, sumColor.y, sumColor.z, sumColor.w);
                    col = Color.Lerp(Color.white, col, sum);
                    colors.Add(col);
                }
                
                mesh.SetColors(colors);
            }

            for (var index = vertexColors.Length - 1; index >= 0; index--)
            {
                LightVertexColor vertexColor = vertexColors[index];
                CoreUtils.Destroy(vertexColor.gameObject);
            }
        }

        private float Influence(float distance, float radius)
        {
            float t = Mathf.Clamp01(1f - distance / radius);

            return t;
        }

        float Remap(float x, float minA, float maxA, float minB, float maxB)
        {
            float t = (x - minA) / (maxA - minA);
            return Mathf.LerpUnclamped(minB, maxB, t);
        }
        
        private Vector3 Remap(Vector3 vector3, Vector3 minA, Vector3 maxA, Vector3 minB, Vector3 maxB)
        {
            Vector3 o = new();
            o.x = Remap(vector3.x, minA.x, maxA.x, minB.x, maxB.x);
            o.y = Remap(vector3.y, minA.y, maxA.y, minB.y, maxB.y);
            o.z = Remap(vector3.z, minA.z, maxA.z, minB.z, maxB.z);
            return o;
        }
    }
}