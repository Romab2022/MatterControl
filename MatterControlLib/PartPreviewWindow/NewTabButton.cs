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

using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;

namespace MatterHackers.MatterControl.PartPreviewWindow
{
	// Holds the space and draws the trailing tabs LowerLeft notch
	public class TabTrailer : GuiWidget
	{
		private SimpleTabs parentTabControl;
		private ThemeConfig theme;

		public ITab LastTab
		{
			get
			{
				ITab lastTab = null;
				var owner = this.Parent;
				if (owner != null)
				{
					foreach (var item in owner.Children)
					{
						if (item is ITab tab)
						{
							lastTab = tab;
						}
					}

				}

				return lastTab;
			}
		}

		public TabTrailer(SimpleTabs parentTabControl, ThemeConfig theme)
		{
			this.parentTabControl = parentTabControl;
			this.theme = theme;
		}

		public override void OnDraw(Graphics2D graphics2D)
		{
			ChromeTab.DrawTabLowerLeft(
				graphics2D,
				this.LocalBounds,
				(parentTabControl.ActiveTab == this.LastTab) ? theme.BackgroundColor : theme.InactiveTabColor);

			base.OnDraw(graphics2D);
		}
	}

	public class NewTabButton : GuiWidget
	{
		private ThemeConfig theme;

		public NewTabButton(ImageBuffer imageBuffer, ThemeConfig theme)
		{
			this.HAnchor = HAnchor.Fit;
			this.VAnchor = VAnchor.Center;
			this.theme = theme;

			IconButton = new IconButton(imageBuffer, theme)
			{
				HAnchor = HAnchor.Left,
				Height = theme.MicroButtonHeight,
				Width = theme.MicroButtonHeight,
				Name = "Create New",
				ToolTipText = "Create New Design".Localize(),
				HoverColor = theme.AccentMimimalOverlay
			};

			this.AddChild(IconButton);
		}

		public ITab LastTab { get; set; }

		public IconButton IconButton { get; }
	}
}