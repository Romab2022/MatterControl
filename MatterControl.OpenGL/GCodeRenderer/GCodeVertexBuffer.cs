﻿/*
Copyright (c) 2018, Lars Brubaker, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using System;
using System.Collections.Generic;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.RenderOpenGl;
using MatterHackers.RenderOpenGl.OpenGl;

namespace MatterHackers.GCodeVisualizer
{
	public class GCodeVertexBuffer : IDisposable
	{
		private int indexID;
		private int indexLength;
		private BeginMode pointMode = BeginMode.Triangles;
		private bool disposed = false;

		private int vertexID;
		private int vertexLength;
		private List<SubTriangleMesh> subMeshs;

		private int[] indexData;
		private ColorVertexData[] colorData;

		public GCodeVertexBuffer(int[] indexData, ColorVertexData[] colorData)
		{
			try
			{
				GL.GenBuffers(1, out vertexID);
				GL.GenBuffers(1, out indexID);
				SetBufferData(ref indexData, ref colorData);
			}
			catch
			{
				this.indexData = indexData;
				this.colorData = colorData;
			}
		}

		private void RenderTriangles(int offset, int count)
		{
			GL.EnableClientState(ArrayCap.ColorArray);
			GL.EnableClientState(ArrayCap.NormalArray);
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.IndexArray);
			GL.DisableClientState(ArrayCap.TextureCoordArray);
			GL.Disable(EnableCap.Texture2D);

			unsafe
			{
				fixed (int* pIndexData = indexData)
				{
					GL.IndexPointer(IndexPointerType.Int, 0, new IntPtr(pIndexData));
					fixed (ColorVertexData* pFixedColorData = colorData)
					{
						byte* pColorData = (byte*)pFixedColorData;
						GL.ColorPointer(4, ColorPointerType.UnsignedByte, ColorVertexData.Stride, new IntPtr(pColorData));
						byte* pNormalData = pColorData + 4;
						GL.NormalPointer(NormalPointerType.Float, ColorVertexData.Stride, new IntPtr(pNormalData));
						byte* pPosition = pNormalData + 12;
						GL.VertexPointer(3, VertexPointerType.Float, ColorVertexData.Stride, new IntPtr(pPosition));
						// GL.DrawArrays(BeginMode.Triangles, ColorVertexData.Stride, Math.Min(colorData.Length, count));
						GL.DrawRangeElements(BeginMode.Triangles,
							0,
							indexData.Length,
							count,
							DrawElementsType.UnsignedInt,
							new IntPtr(pIndexData + offset));
					}
				}
			}

			GL.DisableClientState(ArrayCap.IndexArray);
			GL.DisableClientState(ArrayCap.NormalArray);
			GL.DisableClientState(ArrayCap.VertexArray);
			GL.DisableClientState(ArrayCap.ColorArray);

			GL.IndexPointer(IndexPointerType.Int, 0, new IntPtr(0));
			GL.ColorPointer(4, ColorPointerType.UnsignedByte, 0, new IntPtr(0));
			GL.NormalPointer(NormalPointerType.Float, 0, new IntPtr(0));
			GL.VertexPointer(3, VertexPointerType.Float, 0, new IntPtr(0));
		}

		private void SetBufferData(ref int[] indexData, ref ColorVertexData[] colorData)
		{
			// Set vertex data
			vertexLength = colorData.Length;
			if (vertexLength > 0)
			{
				GL.BindBuffer(BufferTarget.ArrayBuffer, vertexID);
				unsafe
				{
					fixed (ColorVertexData* dataPointer = colorData)
					{
						GL.BufferData(BufferTarget.ArrayBuffer, colorData.Length * ColorVertexData.Stride, (IntPtr)dataPointer, BufferUsageHint.StaticDraw);
					}
				}
			}

			// Set index data
			indexLength = indexData.Length;
			if (indexLength > 0)
			{
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexID);
				unsafe
				{
					fixed (int* dataPointer = indexData)
					{
						GL.BufferData(BufferTarget.ElementArrayBuffer, indexData.Length * sizeof(int), (IntPtr)dataPointer, BufferUsageHint.StaticDraw);
					}
				}
			}
		}

		public void RenderRange(int offset, int count)
		{
			if (vertexID == 0)
			{
				// not allocated don't render
				RenderTriangles(offset, count);
			}
			else
			{
				RenderBufferData(offset, count);
			}
		}

		private void RenderBufferData(int offset, int count)
		{
			GL.EnableClientState(ArrayCap.ColorArray);
			GL.EnableClientState(ArrayCap.NormalArray);
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.DisableClientState(ArrayCap.TextureCoordArray);
			GL.Disable(EnableCap.Texture2D);

			GL.EnableClientState(ArrayCap.IndexArray);

			GL.BindBuffer(BufferTarget.ArrayBuffer, vertexID);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexID);

			GL.ColorPointer(4, ColorPointerType.UnsignedByte, ColorVertexData.Stride, new IntPtr(0));
			GL.NormalPointer(NormalPointerType.Float, ColorVertexData.Stride, new IntPtr(4));
			GL.VertexPointer(3, VertexPointerType.Float, ColorVertexData.Stride, new IntPtr(4 + 3 * 4));

			// ** Draw **
			GL.DrawRangeElements(
				pointMode,
				0,
				indexLength,
				count,
				DrawElementsType.UnsignedInt,
				new IntPtr(offset * 4));

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			GL.DisableClientState(ArrayCap.IndexArray);

			GL.DisableClientState(ArrayCap.VertexArray);
			GL.DisableClientState(ArrayCap.NormalArray);
			GL.DisableClientState(ArrayCap.ColorArray);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (vertexID == 0)
			{
				// not allocated don't dispose
				return;
			}

			// release unmanaged resources
			if (!disposed)
			{
				UiThread.RunOnIdle(() =>
				{
					GL.DeleteBuffers(1, ref vertexID);
					GL.DeleteBuffers(1, ref indexID);
				});

				disposed = true;
			}

			if (disposing)
			{
				// release other Managed objects
				// if (resource!= null) resource.Dispose();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		~GCodeVertexBuffer()
		{
			Dispose(false);
		}
	}
}