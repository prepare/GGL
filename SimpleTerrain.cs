﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GGL;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GGL
{
    public class SimpleTerrain : Mesh
    {
        int size;
        int sizeScale = 1;
        float heightScale = 0.2f;

        float[,] heights;

        Texture alpha;
        Texture terre;
        Texture grass;
        Texture stone;
        Texture rock;

        float tile = 10f;

        Shader shader;

        public SimpleTerrain(float[,] _heights)
        {
            heights = _heights;
            size = (int)Math.Sqrt(heights.Length) - 1;

            buildVertices();
            buildTriangles();

            Prepare();

            alpha = new Texture(directories.rootDir + @"Images/texture/terrain/alpha1.png",true);
            terre = new Texture(directories.rootDir + @"Images/texture/terrain/burnt_sand_lighter.png", true);
            grass = new Texture(directories.rootDir + @"Images/texture/terrain/grass_001.jpg", true);
            stone = new Texture(directories.rootDir + @"Images/texture/terrain/stone1.jpg", true);
            rock = new Texture(directories.rootDir + @"Images/texture/terrain/rock1.jpg", true);

            shader = new ExternalShader("terrainSplatting", "terrainSplatting", "");

            GL.Uniform1(GL.GetUniformLocation(shader, "Alpha"), 0);
            GL.Uniform1(GL.GetUniformLocation(shader, "Terre"), 1);
            GL.Uniform1(GL.GetUniformLocation(shader, "Grass"), 2);
            GL.Uniform1(GL.GetUniformLocation(shader, "Stone"), 3);
            GL.Uniform1(GL.GetUniformLocation(shader, "Rock"), 4);
            GL.Uniform1(GL.GetUniformLocation(shader, "tile"), 1, ref tile);
        }
        
        int nbVertices
        {
            get { return (int)Math.Pow(size + 1, 2.0); }
        }
        
        int nbTriangles
        {
            get { return (int)Math.Pow(size, 2.0) * 2; }
        }

        void buildVertices()
        {
            int x = 0,
                y = 0;

            Vertices = new Vertex[nbVertices];

            
            for (y = 0; y < size + 1; y++)
            {
                for (x = 0; x < size + 1; x++)
                {
                    Vertex v = new Vertex();
                    v.position = new Vector3(x * sizeScale, y * sizeScale, heights[x, y] * heightScale);
                    v.TexCoord = new Vector2((float)x / size, (float)y / size);
                    v.Normal = new Vector3(0, 0, 1);
                    Vertices[x + y * (size + 1)] = v;
                }
            }

            List<Vector3>[] normals = new List<Vector3>[nbVertices];
            for (x = 0; x < size + 1; x++)
            {
                for (y = 0; y < size + 1; y++)
                {
                    normals[x + y * (size + 1)] = new List<Vector3>();
                }
            }
            for (x = 0; x < size; x++)
            {
                for (y = 0; y < size; y++)
                {
                    Vector3 v1 = Vertices[x + y * (size + 1)].position;
                    Vector3 v2 = Vertices[x + 1 + y * (size + 1)].position;
                    Vector3 v3 = Vertices[x + (y + 1) * (size + 1)].position;

                    Vector3 dir = Vector3.Cross(v2 - v1, v3 - v1);
                    dir.Normalize();
                    normals[x + y * (size + 1)].Add(dir);
                    normals[x + 1 + y * (size + 1)].Add(dir);
                    normals[x + (y + 1) * (size + 1)].Add(dir);

                    v1 = Vertices[x + (y + 1) * (size + 1)].position;
                    v2 = Vertices[x + 1 + y * (size + 1)].position;
                    v3 = Vertices[x + 1 + (y + 1) * (size + 1)].position;

                    dir = Vector3.Cross(v2 - v1, v3 - v1);
                    dir.Normalize();
                    normals[x + 1 + y * (size + 1)].Add(dir);
                    normals[x + 1 + (y + 1) * (size + 1)].Add(dir);
                    normals[x + (y + 1) * (size + 1)].Add(dir);
                }
            }

            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 moy = new Vector3(0, 0, 0);

                if (normals[i] != null)
                {
                    foreach (Vector3 v in normals[i])
                    {
                        moy += v;
                    }
                    Vertices[i].Normal = Vector3.Normalize(moy / normals[i].Count);
                }
            }            
                        
        }

        void buildTriangles()
        {
            FaceGroup faces = new FaceGroup();

            faces.Quads = new Quad[0];
            faces.Triangles = new Triangle[nbTriangles];

            int x = 0;
            int y = 0;

            while (y < size)
            {
                while (x < size)
                {
                    Triangle t = new Triangle();

                    t.Index0 = x + y * (size + 1);
                    t.Index1 = x + 1 + y * (size + 1);
                    t.Index2 = x + (y + 1) * (size + 1);
                    faces.Triangles[(x * 2 + y * 2 * (size))] = t;

                    t = new Triangle();
                    t.Index0 = x + (y + 1) * (size + 1);
                    t.Index1 = x + 1 + y * (size + 1);
                    t.Index2 = x + 1 + (y + 1) * (size + 1);

                    faces.Triangles[(x * 2 + y * 2 * (size)) + 1] = t;

                    x++;
                }
                x = 0;
                y++;
            }

            Faces.Add(faces);
        }

        public override void Render()
        {
            int savePgm = GL.GetInteger(GetPName.CurrentProgram);

            GL.UseProgram(shader);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, alpha);
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, terre);
            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.Texture2D, grass);
            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.Texture2D, stone);
            GL.ActiveTexture(TextureUnit.Texture4);
            GL.BindTexture(TextureTarget.Texture2D, rock);
            base.Render();

            GL.UseProgram(savePgm);
        }
    }
}
