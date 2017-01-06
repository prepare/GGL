﻿//
//  Mesh.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using OpenTK;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Tetra.DynamicShading
{
	[Serializable]
	public struct MeshData
	{
		public Vector2[] TexCoords;
		public Vector3[] Normals;

		public MeshData(Vector2[] _texCoord, Vector3[] _normals)
		{
			TexCoords = _texCoord;
			Normals = _normals;
		}
	}
	public struct WeightedMeshData
	{
		public Vector2[] TexCoords;
		public Vector3[] Normals;
		public Vector4[] Weights;

		public WeightedMeshData(Vector2[] _texCoord, Vector3[] _normals, Vector4[] _weights)
		{
			TexCoords = _texCoord;
			Normals = _normals;
			Weights = _weights;
		}
	}
	[Serializable]
	public abstract class Mesh{
		public string Name = "unamed";

		public Vector3[] Positions;
		public ushort[] Indices;

		public abstract Type DataType { get; }

		public Mesh(Vector3[] _positions, ushort[] _indices){
			Indices = _indices;
			Positions = _positions;
		}

		public static Mesh<MeshData> CreateQuad(float x, float y, float z, float width, float height, float TileX = 1f, float TileY = 1f)
		{
			return new Mesh<MeshData> (
			new Vector3[] {
				new Vector3 (x - width / 2, y + height / 2, z),
				new Vector3 (x - width / 2, y - height / 2, z),
				new Vector3 (x + width / 2, y + height / 2, z),
				new Vector3 (x + width / 2, y - height / 2, z)},
			new MeshData (new Vector2[] {
				new Vector2 (0, TileY),
				new Vector2 (0, 0),
				new Vector2 (TileX, TileY),
				new Vector2 (TileX, 0)},
			new Vector3[] {
				Vector3.UnitZ,
				Vector3.UnitZ,
				Vector3.UnitZ,
				Vector3.UnitZ}),
			new ushort[] { 0, 1, 2, 3 });

		}

	}
	[Serializable]
	public class Mesh<T> : Mesh where T : struct
	{
		public T Datas;
		[NonSerialized]public int DuplicatedVerticesRemoved;

		//public Dictionary<string, int[]> Indices;
		public Mesh (Vector3[] _positions, T datas, ushort[] _indices)
			: base(_positions, _indices)
		{
			Datas = datas;
			DuplicatedVerticesRemoved = 0;
		}

		public void SaveAsBinary(string fileName){
			using (FileStream ms = new FileStream (fileName, FileMode.Create)) {
				BinaryFormatter formatter = new BinaryFormatter();
				formatter.Serialize (ms, this);
			}
		}
		public static Mesh<T> LoadBinary (string fileName){
			using (Stream stream = GGL.FileSystemHelpers.GetStreamFromPath (fileName)) {
				BinaryFormatter formatter = new BinaryFormatter();
				return (Mesh<T>)formatter.Deserialize (stream);
			}
		}
		public static Mesh<T> Load (string fileName)
		{
			OBJLoadingCache obj = new OBJLoadingCache ();

			string name = "unamed";

			using (Stream stream = GGL.FileSystemHelpers.GetStreamFromPath (fileName)) {
				using (StreamReader Reader = new StreamReader (stream)) {
					System.Globalization.CultureInfo savedCulture = Thread.CurrentThread.CurrentCulture;
					Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

					string line;
					while ((line = Reader.ReadLine ()) != null) {
						line = line.Trim (' ');
						line = line.Replace ("  ", " ");

						string[] parameters = line.Split (' ');

						switch (parameters [0]) {
						case "o":
							name = parameters [1];
							break;
						case "p": // Point
							break;
						case "v": // Vertex
							float x = float.Parse (parameters [1]);
							float y = float.Parse (parameters [2]);
							float z = float.Parse (parameters [3]);

							obj.objPositions.Add (new Vector3 (x, y, z));
							break;
						case "vt": // TexCoord
							float u = float.Parse (parameters [1]);
							float v = float.Parse (parameters [2]);
							obj.objTexCoords.Add (new Vector2 (u, v));
							break;

						case "vn": // Normal
							float nx = float.Parse (parameters [1]);
							float ny = float.Parse (parameters [2]);
							float nz = float.Parse (parameters [3]);
							obj.objNormals.Add (new Vector3 (nx, ny, nz));
							break;

						case "f":
							switch (parameters.Length) {
							case 4:

								obj.ParseFaceParameter (parameters [1]);
								obj.ParseFaceParameter (parameters [2]);
								obj.ParseFaceParameter (parameters [3]);
								break;

							case 5:
								obj.ParseFaceParameter (parameters [1]);
								obj.ParseFaceParameter (parameters [2]);
								obj.ParseFaceParameter (parameters [3]);
								obj.ParseFaceParameter (parameters [4]);
								break;
							}
							break;
						}
					}
					Thread.CurrentThread.CurrentCulture = savedCulture;
				}
			}
			object dataTmp = Activator.CreateInstance<T>();
			foreach (FieldInfo fi in typeof(T).GetFields()) {

				switch (fi.Name.ToLowerInvariant()) {
				case "texcoords":
					fi.SetValue (dataTmp, obj.lTexCoords.ToArray ());
					break;
				case "normals":
					fi.SetValue (dataTmp, obj.lNormals.ToArray ());
					break;
				case "weights":
					break;
				default:
					Console.WriteLine ("Unknown field '{0}' in Mesh datas '{1}", fi.Name, typeof(T).Name);
					break;
				}
			}

			Mesh<T> tmp = new Mesh<T> (obj.lPositions.ToArray (), (T)dataTmp, obj.lIndices.ToArray ());
			tmp.Name = name;
			tmp.DuplicatedVerticesRemoved = obj.dupVertices;
			return tmp;
		}

		class OBJLoadingCache {
			public List<Vector3> objPositions = new List<Vector3>();
			public List<Vector3> objNormals = new List<Vector3>();
			public List<Vector2> objTexCoords = new List<Vector2>();
			public List<Vector3> lPositions = new List<Vector3>();
			public List<Vector3> lNormals = new List<Vector3>();
			public List<Vector2> lTexCoords = new List<Vector2>();
			public List<ushort> lIndices = new List<ushort>();
			public int dupVertices = 0;

			public void ParseFaceParameter(string faceParameter)
			{
				Vector3 vertex = new Vector3();
				Vector2 texCoord = new Vector2();
				Vector3 normal = new Vector3();

				string[] parameters = faceParameter.Split('/');

				int vertexIndex = int.Parse(parameters[0]);
				if (vertexIndex < 0) vertexIndex = objPositions.Count + vertexIndex;
				else vertexIndex = vertexIndex - 1;
				vertex = objPositions[vertexIndex];

				if (parameters.Length > 1)
				{
					int texCoordIndex;
					if (int.TryParse(parameters[1], out texCoordIndex))
					{
						if (texCoordIndex < 0) texCoordIndex = objTexCoords.Count + texCoordIndex;
						else texCoordIndex = texCoordIndex - 1;
						texCoord = objTexCoords[texCoordIndex];
					}
				}

				if (parameters.Length > 2)
				{
					int normalIndex;
					if (int.TryParse(parameters[2], out normalIndex))
					{
						if (normalIndex < 0) normalIndex = objNormals.Count + normalIndex;
						else normalIndex = normalIndex - 1;
						normal = objNormals[normalIndex];
					}
				}

				//prevent duplicate vertex
				for (int i = 0; i < lPositions.Count; i++) {
					if (lPositions [i] != vertex)
						continue;
					if (lTexCoords [i] != texCoord)
						continue;
					if (lNormals [i] != normal)
						continue;
					lIndices.Add ((ushort)i);
					dupVertices++;
					return;
				}

				lPositions.Add(vertex);
				lTexCoords.Add(texCoord);
				lNormals.Add(normal);

				lIndices.Add ((ushort)(lPositions.Count - 1));
			}
		}

		#region implemented abstract members of Mesh

		public override Type DataType {
			get { return typeof(T);	}
		}

		#endregion
	}



	#region .OBJ Loading
	public static class OBJMeshLoader{
		static List<Vector3> objPositions;
		static List<Vector3> objNormals;
		static List<Vector2> objTexCoords;
		static List<Vector3> lPositions;
		static List<Vector3> lNormals;
		static List<Vector2> lTexCoords;
		static List<ushort> lIndices;
		static List<Dictionary<string, float>> objWeights;
		static List<Dictionary<string, float>> lWeights;
		static List<string> objGroups;
		static List<string> objBones;

		public static Mesh<T> Load<T>(string fileName) where T : struct
		{
			objPositions = new List<Vector3>();
			objNormals = new List<Vector3>();
			objTexCoords = new List<Vector2>();
			lPositions = new List<Vector3>();
			lNormals = new List<Vector3>();
			lTexCoords = new List<Vector2>();
			lIndices = new List<ushort> ();
			objWeights = new List<Dictionary<string, float>> ();
			lWeights = new List<Dictionary<string, float>> ();
			objBones = new List<string> ();
			objGroups = new List<string> ();

			string name = "unamed";
			using (Stream stream = GGL.FileSystemHelpers.GetStreamFromPath (fileName)) {
				using (StreamReader Reader = new StreamReader (stream)) {
					System.Globalization.CultureInfo savedCulture = Thread.CurrentThread.CurrentCulture;
					Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

					string line;
					while ((line = Reader.ReadLine ()) != null) {
						line = line.Trim (' ');
						line = line.Replace ("  ", " ");

						string[] parameters = line.Split (' ');

						switch (parameters [0]) {
						case "o":
							name = parameters [1];
							break;
						case "p": // Point
							break;
						case "v": // Vertex
							float x = float.Parse (parameters [1]);
							float y = float.Parse (parameters [2]);
							float z = float.Parse (parameters [3]);

							objPositions.Add (new Vector3 (x, y, z));
							break;
						case "vt": // TexCoord
							float u = float.Parse (parameters [1]);
							float v = float.Parse (parameters [2]);
							objTexCoords.Add (new Vector2 (u, v));
							break;

						case "vn": // Normal
							float nx = float.Parse (parameters [1]);
							float ny = float.Parse (parameters [2]);
							float nz = float.Parse (parameters [3]);
							objNormals.Add (new Vector3 (nx, ny, nz));
							break;

						case "f":
							switch (parameters.Length) {
							case 4:

								lIndices.Add (ParseFaceParameter (parameters [1]));
								lIndices.Add (ParseFaceParameter (parameters [2]));
								lIndices.Add (ParseFaceParameter (parameters [3]));
								break;

							case 5:
								lIndices.Add (ParseFaceParameter (parameters [1]));
								lIndices.Add (ParseFaceParameter (parameters [2]));
								lIndices.Add (ParseFaceParameter (parameters [3]));
								lIndices.Add (ParseFaceParameter (parameters [4]));
								break;
							}
							break;
						case "w":
							int vidx = int.Parse (parameters [1]);
							Dictionary<string, float> weights = new Dictionary<string, float> ();
							if (objWeights.Count != vidx)
								throw new Exception ("not all vertices has weight datas");
							objWeights.Add (weights);
							int ptrW = 2;
							while (ptrW < parameters.Length) {
								//make list of group's names
								if (!objGroups.Contains (parameters [ptrW]))
									objGroups.Add (parameters [ptrW]);
								weights [parameters [ptrW]] = float.Parse (parameters [ptrW + 1]);
								ptrW += 2;
							}
							break;
						case "b":
							if (!objBones.Contains (parameters [1]))
								objBones.Add (parameters [1]);
							break;
						}
					}
					Thread.CurrentThread.CurrentCulture = savedCulture;
				}
			}
			Mesh tmp = null;

			if (objWeights.Count == 0) {
				MeshData md;
				md.Normals = lNormals.ToArray ();
				md.TexCoords = lTexCoords.ToArray ();

				tmp = new Mesh<MeshData> (lPositions.ToArray (), md, lIndices.ToArray ());
			} else {
				string[] bones;
				if (objBones.Count > 0)
					bones = objBones.ToArray ();
				else
					bones = objGroups.ToArray ();

				WeightedMeshData md;
				md.Normals = lNormals.ToArray ();
				md.TexCoords = lTexCoords.ToArray ();

				if (bones.Length > 4)
					throw new Exception ("Error loading obj: maximum 4 groups in weighted obj");

				md.Weights = new Vector4[lWeights.Count];

				for (int i = 0; i < lWeights.Count; i++) {
					float[] weights = new float[4];
					for (int j = 0; j < bones.Length; j++) {
						if (lWeights[i].ContainsKey(bones [j]))
							weights[j] = lWeights[i][bones [j]];
					}
					md.Weights [i] = new Vector4 (weights [0], weights [1], weights [2], weights [3]);
				}

				tmp = new Mesh<WeightedMeshData> (lPositions.ToArray (), md, lIndices.ToArray ());
			}

			tmp.Name = name;

			objPositions.Clear();
			objNormals.Clear();
			objTexCoords.Clear();
			lPositions.Clear();
			lNormals.Clear();
			lTexCoords.Clear();
			lIndices.Clear();
			objGroups.Clear();
			objBones.Clear ();
			objWeights.Clear ();
			lWeights.Clear ();

			return tmp as Mesh<T>;
		}

		static ushort ParseFaceParameter(string faceParameter)
		{
			Vector3 vertex = new Vector3();
			Vector2 texCoord = new Vector2();
			Vector3 normal = new Vector3();

			string[] parameters = faceParameter.Split('/');

			int vertexIndex = int.Parse(parameters[0]);
			if (vertexIndex < 0) vertexIndex = objPositions.Count + vertexIndex;
			else vertexIndex = vertexIndex - 1;
			vertex = objPositions[vertexIndex];

			if (parameters.Length > 1)
			{
				int texCoordIndex;
				if (int.TryParse(parameters[1], out texCoordIndex))
				{
					if (texCoordIndex < 0) texCoordIndex = objTexCoords.Count + texCoordIndex;
					else texCoordIndex = texCoordIndex - 1;
					texCoord = objTexCoords[texCoordIndex];
				}
			}

			if (parameters.Length > 2)
			{
				int normalIndex;
				if (int.TryParse(parameters[2], out normalIndex))
				{
					if (normalIndex < 0) normalIndex = objNormals.Count + normalIndex;
					else normalIndex = normalIndex - 1;
					normal = objNormals[normalIndex];
				}
			}

			lPositions.Add(vertex);
			lTexCoords.Add(texCoord);
			lNormals.Add(normal);
			if (objWeights.Count > 0)
				lWeights.Add (objWeights[vertexIndex]);

			int index = lPositions.Count-1;
			return (ushort)index;
		}
	}
	#endregion
}

