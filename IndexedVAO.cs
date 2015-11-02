﻿using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;

namespace GGL
{
	public class IndexedVAO<T> : IDisposable 
		where T : struct
	{
		public int vaoHandle,
		positionVboHandle,
		texVboHandle,
		eboHandle;

		T[] positions;
		public int[] indicesVboData;
		Vector2[] texCoords;

		public IndexedVAO (T[] _positions, Vector2[] _texCoord, int[] _indices)
		{
			positions = _positions;
			texCoords = _texCoord;
			indicesVboData = _indices;

			CreateVBOs ();
			CreateVAOs ();
		}

		void deleteVAOs()
		{
			GL.DeleteBuffer (positionVboHandle);
			GL.DeleteBuffer (texVboHandle);
			GL.DeleteBuffer (eboHandle);
			GL.DeleteVertexArray (vaoHandle);
		}

		void CreateVBOs()
		{
			positionVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.BufferData<T>(BufferTarget.ArrayBuffer,
				new IntPtr(positions.Length * Marshal.SizeOf(typeof(T))),
				positions, BufferUsageHint.StaticDraw);

			texVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, texVboHandle);
			GL.BufferData<Vector2>(BufferTarget.ArrayBuffer,
				new IntPtr(texCoords.Length * Vector2.SizeInBytes),
				texCoords, BufferUsageHint.StaticDraw);
			//
			eboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);
			GL.BufferData(BufferTarget.ElementArrayBuffer,
				new IntPtr(sizeof(uint) * indicesVboData.Length),
				indicesVboData, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}

		void CreateVAOs()
		{
			vaoHandle = GL.GenVertexArray();
			GL.BindVertexArray(vaoHandle);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);

			if (typeof(T) == typeof(Vector2))
				GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);
			else if (typeof(T) == typeof(Vector3))
				GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);


			GL.EnableVertexAttribArray(1);
			GL.BindBuffer(BufferTarget.ArrayBuffer, texVboHandle);
			GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, true, Vector2.SizeInBytes, 0);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

			GL.BindVertexArray(0);
		}
			
		public void Render(PrimitiveType _primitiveType){
			GL.BindVertexArray(vaoHandle);
			GL.DrawElements(_primitiveType, indicesVboData.Length,
				DrawElementsType.UnsignedInt, IntPtr.Zero);	
			GL.BindVertexArray (0);
		}


		#region IDisposable implementation
		public void Dispose ()
		{
			deleteVAOs ();
		}
		#endregion
	}
}

