﻿/*
Copyright (c) 2014, Lars Brubaker
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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.VersionManagement;

namespace MatterHackers.MatterControl
{
    public class UpdateControlData
    {
        public enum UpdateStatusStates { Unknown, UpdateAvailable, UpdateDownloading, UpdateDownloaded }; 
        public RootedObjectEventHandler UpdateStatusChanged = new RootedObjectEventHandler();

        UpdateStatusStates updateStatus;
        public UpdateStatusStates UpdateStatus
        {
            get { return updateStatus; }
            set 
            {
                if (updateStatus != value)
                {
                    updateStatus = value;
                    OnUpdateStatusChanged(null);
                }
            }
        }

        static UpdateControlData instance;
        static public UpdateControlData Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UpdateControlData();
                }

                return instance;
            }
        }

        private UpdateControlData()
        {
        }

        public void OnUpdateStatusChanged(EventArgs e)
        {
            UpdateStatusChanged.CallEvents(this, e);
        }
    }
}
