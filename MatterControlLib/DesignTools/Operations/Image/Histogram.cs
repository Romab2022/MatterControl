﻿/*
Copyright (c) 2017, Lars Brubaker, John Lewin
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
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.Image.ThresholdFunctions;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterControl.DesignTools
{
	public class Histogram
	{
		private ImageBuffer _histogramRawCache = new ImageBuffer(256, 100);
		private ThemeConfig theme;

		public double RangeStart { get; set; } = 0;

		public double RangeEnd { get; set; } = .9;

		private Color GetRGBA(byte[] buffer, int offset)
		{
			return new Color(buffer[offset + 2], buffer[offset + 1], buffer[offset + 0], buffer[offset + 3]);
		}

		public event EventHandler RangeChanged;
		
		public event EventHandler EditComplete;

		public void RebuildAlphaImage(ImageBuffer sourceImage, ImageBuffer alphaImage)
		{
			if (sourceImage == null)
			{
				return;
			}

			// build the alpha image
			if (alphaImage == null)
			{
				alphaImage = new ImageBuffer(sourceImage.Width, sourceImage.Height);
			}
			else if (alphaImage.Width != sourceImage.Width
				|| alphaImage.Height != sourceImage.Height)
			{
				alphaImage.Allocate(sourceImage.Width, sourceImage.Height, sourceImage.BitDepth, sourceImage.GetRecieveBlender());
			}

			var startInt = (int)(RangeStart * 255);
			var endInt = (int)(RangeEnd * 255);
			var rangeInt = (int)Math.Max(1, (RangeEnd - RangeStart) * 255);

			byte GetAlphaFromIntensity(byte r, byte g, byte b)
			{
				// return (color.Red0To1 * 0.2989) + (color.Green0To1 * 0.1140) + (color.Blue0To1 * 0.5870);
				var alpha = (r * 76 + g * 29 + b * 150) / 255;
				if (alpha < startInt)
				{
					return 0;
				}
				else if (alpha > endInt)
				{
					return 0;
				}
				else
				{
					var s1 = 255 - Math.Min(255, ((alpha - startInt) * 255 / rangeInt));

					return (byte)s1;
				}
			}

			byte[] sourceBuffer = sourceImage.GetBuffer();
			byte[] destBuffer = alphaImage.GetBuffer();
			Parallel.For(0, sourceImage.Height, (y) =>
			{
				int imageOffset = sourceImage.GetBufferOffsetY(y);

				for (int x = 0; x < sourceImage.Width; x++)
				{
					int imageBufferOffsetWithX = imageOffset + x * 4;
					var r = sourceBuffer[imageBufferOffsetWithX + 0];
					var g = sourceBuffer[imageBufferOffsetWithX + 1];
					var b = sourceBuffer[imageBufferOffsetWithX + 2];
					destBuffer[imageBufferOffsetWithX + 0] = r;
					destBuffer[imageBufferOffsetWithX + 1] = g;
					destBuffer[imageBufferOffsetWithX + 2] = b;
					destBuffer[imageBufferOffsetWithX + 3] = GetAlphaFromIntensity(r, g, b);
				}
			});

			alphaImage.MarkImageChanged();
		}

		public void BuildHistogramFromImage(ImageBuffer image)
		{
			// build the histogram cache
			_histogramRawCache = new ImageBuffer(256, 100);
			var counts = new int[_histogramRawCache.Width];
			IThresholdFunction function = new MapOnMaxIntensity(RangeStart, RangeEnd);
			function = new HueThresholdFunction(RangeStart, RangeEnd);

			byte[] buffer = image.GetBuffer();
			for (int y = 0; y < image.Height; y++)
			{
				int imageBufferOffset = image.GetBufferOffsetY(y);

				for (int x = 0; x < image.Width; x++)
				{
					int imageBufferOffsetWithX = imageBufferOffset + x * 4;
					var color = GetRGBA(buffer, imageBufferOffsetWithX);
					counts[(int)(function.Transform(color) * (_histogramRawCache.Width - 1))]++;
				}
			}

			double max = counts.Select((value, index) => new { value, index })
				.OrderByDescending(vi => vi.value)
				.First().value;
			var graphics2D2 = _histogramRawCache.NewGraphics2D();
			graphics2D2.Clear(ApplicationController.Instance.Theme.SlightShade);

			var graphShape = new VertexStorage();
			graphShape.MoveTo(0, 0);
			for (int i = 0; i < 256; i++)
			{
				graphShape.LineTo(i, Easing.Exponential.Out(counts[i] / max) * _histogramRawCache.Height);
			}
			graphShape.LineTo(image.Width, 0);
			graphics2D2.Render(graphShape, 0, 0, theme.TextColor);
		}

		public GuiWidget NewEditWidget(ThemeConfig theme)
		{
			this.theme = theme;
			var histogramWidget = new GuiWidget()
			{
				HAnchor = HAnchor.Stretch,
				Height = 60 * GuiWidget.DeviceScale,
				Margin = 5,
			};

			var handleWidth = 10 * GuiWidget.DeviceScale;
			var histogramBackground = new GuiWidget()
			{
				HAnchor = HAnchor.Stretch,
				VAnchor = VAnchor.Stretch,
				Margin = new BorderDouble(handleWidth, 0)
			};

			histogramBackground.AfterDraw += (s, e) =>
			{
				var rangeStart = RangeStart;
				var rangeEnd = RangeEnd;
				var graphics2D = e.Graphics2D;
				graphics2D.Render(_histogramRawCache, 0, 0);
				var background = _histogramRawCache;
				graphics2D.FillRectangle(rangeStart * background.Width, 0, rangeEnd * background.Width, background.Height, theme.PrimaryAccentColor.WithAlpha(60));
			};

			histogramWidget.AddChild(histogramBackground);

			void RenderHandle(Graphics2D g, double s, double e)
			{
				var w = g.Width;
				var h = g.Height;
				g.Line(w * e, 0, w * e, h, theme.TextColor);
				var leftEdge = new VertexStorage();
				leftEdge.MoveTo(w * e, h * .80);
				leftEdge.curve3(w * e, h * .70, w * .5, h * .70);
				leftEdge.curve3(w * s, h * .60);
				leftEdge.LineTo(w * s, h * .40);
				leftEdge.curve3(w * s, h * .30, w * .5, h * .30);
				leftEdge.curve3(w * e, h * .20);
				g.Render(new FlattenCurves(leftEdge), theme.PrimaryAccentColor);
				g.Line(w * .35, h * .6, w * .35, h * .4, theme.BackgroundColor);
				g.Line(w * .65, h * .6, w * .65, h * .4, theme.BackgroundColor);
			}

			var leftHandle = new ImageWidget((int)(handleWidth), (int)histogramWidget.Height);
			leftHandle.Position = new Vector2(RangeStart * _histogramRawCache.Width, 0);
			var image = leftHandle.Image;
			RenderHandle(image.NewGraphics2D(), 0, 1);
			histogramWidget.AddChild(leftHandle);

			bool leftDown = false;
			var leftX = 0.0;
			leftHandle.MouseDown += (s, e) =>
			{
				if (e.Button == MouseButtons.Left)
				{
					leftDown = true;
					leftX = e.Position.X;
				}
			};
			leftHandle.MouseMove += (s, e) =>
			{
				if (leftDown)
				{
					var offset = e.Position.X - leftX;
					var newStart = RangeStart + offset / _histogramRawCache.Width;
					newStart = agg_basics.Clamp(newStart, 0, RangeEnd);
					if (RangeStart != newStart)
					{
						RangeStart = newStart;
						leftHandle.Position = new Vector2(RangeStart * _histogramRawCache.Width, 0);
						RangeChanged?.Invoke(this, null);
					}
				}
			};
			leftHandle.MouseUp += (s, e) =>
			{
				if (leftDown)
				{
					leftDown = false;
					EditComplete?.Invoke(this, null);
				}
			};

			var rightHandle = new ImageWidget((int)(handleWidth), (int)histogramWidget.Height);
			rightHandle.Position = new Vector2(RangeEnd * _histogramRawCache.Width + handleWidth, 0);
			image = rightHandle.Image;
			RenderHandle(image.NewGraphics2D(), 1, 0);
			histogramWidget.AddChild(rightHandle);

			bool rightDown = false;
			var rightX = 0.0;
			rightHandle.MouseDown += (s, e) =>
			{
				if (e.Button == MouseButtons.Left)
				{
					rightDown = true;
					rightX = e.Position.X;
				}
			};
			rightHandle.MouseMove += (s, e) =>
			{
				if (rightDown)
				{
					var offset = e.Position.X - rightX;
					var newEnd = RangeEnd + offset / _histogramRawCache.Width;
					newEnd = agg_basics.Clamp(newEnd, RangeStart, 1);
					if (RangeEnd != newEnd)
					{
						RangeEnd = newEnd;
						rightHandle.Position = new Vector2(RangeEnd * _histogramRawCache.Width + handleWidth, 0);
						RangeChanged?.Invoke(this, null);
					}
				}
			};
			rightHandle.MouseUp += (s, e) =>
			{
				if (rightDown)
				{
					rightDown = false;
					EditComplete?.Invoke(this, null);
				}
			};

			return histogramWidget;
		}
	}
}