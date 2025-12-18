using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Rendering.MarchingCubes
{
    public abstract class Marching
    {
        /// <summary>
        /// The surface value in the voxels. Normally set to 0. 
        /// </summary>
        public float Surface { get; set; }

        /// <summary>
        /// 
        /// </summary>
        private float[] Cube { get; set; }

        /// <summary>
        /// Winding order of triangles use 2,1,0 or 0,1,2
        /// </summary>
        protected int[] WindingOrder { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="surface"></param>
        public Marching(float surface)
        {
            Surface = surface;
            Cube = new float[8];
            WindingOrder = new int[] { 0, 1, 2 };
        }

        public virtual void Generate(Texture3D voxels, IList<Vector3> verts, IList<int> indices, IList<Vector3> normals = null)
        {
            int width = voxels.width;
            int height = voxels.height;
            int depth = voxels.depth;

            UpdateWindingOrder();

            int x, y, z, i;
            int ix, iy, iz;
            for (x = 0; x < width - 1; x++)
            {
                for (y = 0; y < height - 1; y++)
                {
                    for (z = 0; z < depth - 1; z++)
                    {
                        //Get the values in the 8 neighbours which make up a cube
                        for (i = 0; i < 8; i++)
                        {
                            ix = x + VertexOffset[i, 0];
                            iy = y + VertexOffset[i, 1];
                            iz = z + VertexOffset[i, 2];

                            Cube[i] = -GetVoxel(ix, iy, iz, voxels);
                        }

                        //Perform algorithm
                        March(x, y, z, Cube, verts, indices);
                    }
                }
            }

            if (normals != null)
            {
                for (int index = 0; index < verts.Count; index++)
                {
                    //Presumes the vertex is in local space where
                    //the min value is 0 and max is width/height/depth.
                    Vector3 p = verts[index];

                    float u = p.x / (width - 1.0f);
                    float v = p.y / (height - 1.0f);
                    float w = p.z / (depth - 1.0f);

                    Vector3 n = GetVoxelDerivative(u, v, w, voxels);

                    normals.Add(n);
                }
            }
        }

        protected float GetVoxel(int x, int y, int z, Texture3D tex)
        {
            int width = tex.width;
            int height = tex.height;
            int depth = tex.depth;

            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            z = Mathf.Clamp(z, 0, depth - 1);
            
            return tex.GetPixel(x, y, z).r;
        }
        
        public float GetVoxel(float u, float v, float w, Texture3D tex)
        {
            int width = tex.width;
            int height = tex.height;
            int depth = tex.depth;
        
            float x = u * (width - 1);
            float y = v * (height - 1);
            float z = w * (depth - 1);

            int xi = (int)Mathf.Floor(x);
            int yi = (int)Mathf.Floor(y);
            int zi = (int)Mathf.Floor(z);

            float v000 = GetVoxel(xi, yi, zi, tex);
            float v100 = GetVoxel(xi + 1, yi, zi, tex);
            float v010 = GetVoxel(xi, yi + 1, zi, tex);
            float v110 = GetVoxel(xi + 1, yi + 1, zi, tex);

            float v001 = GetVoxel(xi, yi, zi + 1, tex);
            float v101 = GetVoxel(xi + 1, yi, zi + 1, tex);
            float v011 = GetVoxel(xi, yi + 1, zi + 1, tex);
            float v111 = GetVoxel(xi + 1, yi + 1, zi + 1, tex);

            float tx = Mathf.Clamp01(x - xi);
            float ty = Mathf.Clamp01(y - yi);
            float tz = Mathf.Clamp01(z - zi);

            //use bilinear interpolation the find these values.
            float v0 = BLerp(v000, v100, v010, v110, tx, ty);
            float v1 = BLerp(v001, v101, v011, v111, tx, ty);

            //Now lerp those values for the final trilinear interpolation.
            return Lerp(v0, v1, tz);
        }
        
        protected Vector3 GetVoxelDerivative(int x, int y, int z, Texture3D tex)
        {
            float dx_p1 = GetVoxel(x + 1, y, z, tex);
            float dy_p1 = GetVoxel(x, y + 1, z, tex);
            float dz_p1 = GetVoxel(x, y, z + 1, tex);

            float dx_m1 = GetVoxel(x - 1, y, z, tex);
            float dy_m1 = GetVoxel(x, y - 1, z, tex);
            float dz_m1 = GetVoxel(x, y, z - 1, tex);

            float dx = (dx_p1 - dx_m1) * 0.5f;
            float dy = (dy_p1 - dy_m1) * 0.5f;
            float dz = (dz_p1 - dz_m1) * 0.5f;

            return new Vector3(dx, dy, dz);
        }

        public Vector3 GetVoxelDerivative(float u, float v, float w, Texture3D tex)
        {
            const float h = 0.005f;
            const float hh = h * 0.5f;
            const float ih = 1.0f / h;

            float dx_p1 = GetVoxel(u + hh, v, w, tex);
            float dy_p1 = GetVoxel(u, v + hh, w, tex);
            float dz_p1 = GetVoxel(u, v, w + hh, tex);
            
            float dx_m1 = GetVoxel(u - hh, v, w, tex);
            float dy_m1 = GetVoxel(u, v - hh, w, tex);
            float dz_m1 = GetVoxel(u, v, w - hh, tex);

            float dx = (dx_p1 - dx_m1) * ih;
            float dy = (dy_p1 - dy_m1) * ih;
            float dz = (dz_p1 - dz_m1) * ih;

            return new Vector3(dx, dy, dz);
        }
        
        private static float Lerp(float v0, float v1, float t)
        {
            return v0 + (v1 - v0) * t;
        }
        private static float BLerp(float v00, float v10, float v01, float v11, float tx, float ty)
        {
            return Lerp(Lerp(v00, v10, tx), Lerp(v01, v11, tx), ty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="voxels"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="depth"></param>
        /// <param name="verts"></param>
        /// <param name="indices"></param>
        public virtual void Generate(IList<float> voxels, int width, int height, int depth, IList<Vector3> verts, IList<int> indices)
        {

            UpdateWindingOrder();

            int x, y, z, i;
            int ix, iy, iz;
            for (x = 0; x < width - 1; x++)
            {
                for (y = 0; y < height - 1; y++)
                {
                    for (z = 0; z < depth - 1; z++)
                    {
                        //Get the values in the 8 neighbours which make up a cube
                        for (i = 0; i < 8; i++)
                        {
                            ix = x + VertexOffset[i, 0];
                            iy = y + VertexOffset[i, 1];
                            iz = z + VertexOffset[i, 2];

                            Cube[i] = voxels[ix + iy * width + iz * width * height];
                        }

                        //Perform algorithm
                        March(x, y, z, Cube, verts, indices);
                    }
                }
            }

        }

        /// <summary>
        /// Update the winding order. 
        /// This determines how the triangles in the mesh are orientated.
        /// </summary>
        protected virtual void UpdateWindingOrder()
        {
            if (Surface > 0.0f)
            {
                WindingOrder[0] = 2;
                WindingOrder[1] = 1;
                WindingOrder[2] = 0;
            }
            else
            {
                WindingOrder[0] = 0;
                WindingOrder[1] = 1;
                WindingOrder[2] = 2;
            }
        }

         /// <summary>
        /// MarchCube performs the Marching algorithm on a single cube
        /// </summary>
        protected abstract void March(float x, float y, float z, float[] cube, IList<Vector3> vertList, IList<int> indexList);

        /// <summary>
        /// GetOffset finds the approximate point of intersection of the surface
        /// between two points with the values v1 and v2
        /// </summary>
        protected virtual float GetOffset(float v1, float v2)
        {
            float delta = v2 - v1;
            return (delta == 0.0f) ? Surface : (Surface - v1) / delta;
        }

        /// <summary>
        /// VertexOffset lists the positions, relative to vertex0, 
        /// of each of the 8 vertices of a cube.
        /// vertexOffset[8][3]
        /// </summary>
        protected static readonly int[,] VertexOffset = new int[,]
	    {
	        {0, 0, 0},{1, 0, 0},{1, 1, 0},{0, 1, 0},
	        {0, 0, 1},{1, 0, 1},{1, 1, 1},{0, 1, 1}
	    };

    }

}
