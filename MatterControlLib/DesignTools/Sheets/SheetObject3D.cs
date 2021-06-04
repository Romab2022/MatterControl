﻿/*
Copyright (c) 2019, Lars Brubaker, John Lewin
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.DataConverters3D;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.PolygonMesh;
using MatterHackers.PolygonMesh.Processors;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.DesignTools
{
	[HideChildrenFromTreeView]
	[HideMeterialAndColor]
	public class SheetObject3D : Object3D, IObject3DControlsProvider
	{
		public SheetData SheetData { get; set; }

		public static async Task<SheetObject3D> Create()
		{
			var item = new SheetObject3D
			{
				SheetData = new SheetData(5, 5)
			};
			await item.Rebuild();
			return item;
		}

		public SheetObject3D()
		{
			using (Stream stlStream = StaticData.Instance.OpenStream(Path.Combine("Stls", "sheet.stl")))
			{
				var mesh = StlProcessing.Load(stlStream, CancellationToken.None);
				var aabb = mesh.GetAxisAlignedBoundingBox();
				mesh.Transform(Matrix4X4.CreateScale(20 / aabb.XSize));
				Mesh = mesh;
			}

			Color = new Color("#117c43");
		}

		public override void OnInvalidate(InvalidateArgs invalidateType)
		{
			if (invalidateType.InvalidateType.HasFlag(InvalidateType.SheetUpdated) && invalidateType.Source == this)
			{
				using (RebuildLock())
				{
					// update the table info
					SheetData.Recalculate();
					// send a message to all our siblings and their children
					SendInvalidateToAll();
				}
			}
			else
			{
				base.OnInvalidate(invalidateType);
			}
		}

		private void SendInvalidateToAll()
		{
			foreach (var sibling in this.Parent.Children)
			{
				SendInvalidateRecursive(sibling);
			}
		}

		private void SendInvalidateRecursive(IObject3D item)
		{
			// process depth first
			foreach(var child in item.Children)
			{
				SendInvalidateRecursive(child);
			}

			// and send the invalidate
			item.Invalidate(new InvalidateArgs(item, InvalidateType.SheetUpdated));
		}

		public static T EvaluateExpression<T>(IObject3D owner, string inputExpression)
		{
			// check if the expression is not an equation (does not start with "=")
			if (inputExpression.Length > 0 && inputExpression[0] != '=')
			{
				// not an equation so try to parse it directly
				if (double.TryParse(inputExpression, out var result))
				{
					if (typeof(T) == typeof(double))
					{
						return (T)(object)result;
					}
					if (typeof(T) == typeof(int))
					{
						return (T)(object)(int)Math.Round(result);
					}
				}
				else
				{
					if (typeof(T) == typeof(double))
					{
						return (T)(object)0.0;
					}
					if (typeof(T) == typeof(int))
					{
						return (T)(object)0;
					}
				}
			}
			
			if (inputExpression.Length > 0 && inputExpression[0] == '=')
			{
				inputExpression = inputExpression.Substring(1);
			}

			// look through all the parents
			foreach (var parent in owner.Parents())
			{
				// then each child of any give parent
				foreach (var sibling in parent.Children)
				{
					// if it is a sheet
					if (sibling != owner
						&& sibling is SheetObject3D sheet)
					{
						// try to manage the cell into the correct data type
						string value = sheet.SheetData.EvaluateExpression(inputExpression);

						if (typeof(T) == typeof(double))
						{
							if (double.TryParse(value, out double doubleValue)
								&& !double.IsNaN(doubleValue)
								&& !double.IsInfinity(doubleValue))
							{
								return (T)(object)doubleValue;
							}
							// else return an error
							return (T)(object).1;
						}

						if (typeof(T) == typeof(int))
						{
							if (double.TryParse(value, out double doubleValue)
								&& !double.IsNaN(doubleValue)
								&& !double.IsInfinity(doubleValue))
							{
								return (T)(object)(int)Math.Round(doubleValue);
							}
							// else return an error
							return (T)(object)1;
						}
					}
				}
			}

			return (T)(object)default(T);
		}

		public void AddObject3DControls(Object3DControlsLayer object3DControlsLayer)
		{
		}
	}
}