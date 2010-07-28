using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MSDefragLib;
using System.Threading;
using System.Timers;
using System.Globalization;

namespace MSDefrag
{
    public partial class MainForm : Form
    {
        #region Constructor

        public MainForm()
        {
            InitializeComponent();

            GuiSettings = new MSDefrag.GuiSettings();
            DefragSettings = new DefragmentationSettings();
        }

        #endregion

        #region Graphics functions

        private void AddFilteredClustersToQueue(IList<MSDefragLib.MapClusterState> filteredClusters)
        {
            if (filteredClusters == null) { return; }

            diskBitmap.AddFilteredClusters(filteredClusters);
        }

        #endregion

        #region Other

        private void StartDefragmentation(EnumDefragType mode)
        {
            Defragmenter = DefragmenterFactory.Create(mode);

            if (Defragmenter == null) { return; }

            diskBitmap.Initialize(GuiSettings);

            Defragmenter.NumFilteredClusters = diskBitmap.NumSquares;

            Defragmenter.StartDefragmentation(DefragSettings.Path);

            RegisterEvents(true);
        }

        private void StopDefragmentation()
        {
            if (Defragmenter == null) { return; }

            Defragmenter.StopDefragmentation(4000);
            RegisterEvents(false);

            Defragmenter = null;

        }

        private void RegisterEvents(Boolean register)
        {
            if (register)
            {
                Defragmenter.ProgressEvent += new EventHandler<ProgressEventArgs>(UpdateProgress);
                Defragmenter.UpdateFilteredDiskMapEvent += new EventHandler<FilteredClusterEventArgs>(UpdateFilteredDiskMap);
                Defragmenter.LogMessageEvent += new EventHandler<LogMessagesEventArgs>(UpdateLogMessages);

            }
            else
            {
                Defragmenter.ProgressEvent -= new EventHandler<ProgressEventArgs>(UpdateProgress);
                Defragmenter.UpdateFilteredDiskMapEvent -= new EventHandler<FilteredClusterEventArgs>(UpdateFilteredDiskMap);
                Defragmenter.LogMessageEvent -= new EventHandler<LogMessagesEventArgs>(UpdateLogMessages);
            }
        }

        private void UpdateProgressBar(Double val)
        {
            progressBar.Value = (Int16)val;

            progressBarText.Text = String.Format(CultureInfo.InstalledUICulture, "{0:P4}", val * 0.01);
        }

        private void UpdateLogMessage(IList<LogMessage> list)
        {
            foreach (LogMessage message in list)
            {
                if (message.LogLevel < 6)
                {
                    Console.WriteLine(DateTime.Now.ToShortTimeString() + "\t Log [" + message.LogLevel + "] : " + message.Message);
                }
            }

            LogMessageLabel.Text = list.Last().Message.Trim();
        }

        #endregion

        #region Event Handling

        private void OnStartDefragmentation(object sender, EventArgs e)
        {
            toolButtonStartDefrag.Enabled = false;
            toolButtonStartSimulation.Enabled = false;
            toolButtonStopDefrag.Enabled = true;

            StartDefragmentation(EnumDefragType.defragTypeDefragmentation);
        }

        private void OnStartSimulation(object sender, EventArgs e)
        {
            toolButtonStartDefrag.Enabled = false;
            toolButtonStartSimulation.Enabled = false;
            toolButtonStopDefrag.Enabled = true;

            StartDefragmentation(EnumDefragType.defragTypeSimulation);
        }

        private void OnStopDefrag(object sender, EventArgs e)
        {
            toolButtonStartDefrag.Enabled = false;
            toolButtonStartSimulation.Enabled = false;
            toolButtonStopDefrag.Enabled = false;

            StopDefragmentation();

            toolButtonStartDefrag.Enabled = true;
            toolButtonStartSimulation.Enabled = true;
            toolButtonStopDefrag.Enabled = false;
        }

        private void UpdateFilteredDiskMap(object sender, EventArgs e)
        {
            FilteredClusterEventArgs ea = e as FilteredClusterEventArgs;

            if (ea != null)
            {
                diskBitmap.AddFilteredClusters(ea.Clusters);
            }
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            ProgressEventArgs ea = e as ProgressEventArgs;

            if (ea != null)
            {
                BeginInvoke(new MethodInvoker(delegate { UpdateProgressBar(ea.Progress); }));
            }
        }

        private void UpdateLogMessages(object sender, EventArgs e)
        {
            LogMessagesEventArgs ea = e as LogMessagesEventArgs;

            if (ea != null)
            {
                BeginInvoke(new MethodInvoker(delegate { UpdateLogMessage(ea.Messages); }));
            }
        }

        private void OnGuiClosing(object sender, FormClosingEventArgs e)
        {
            StopDefragmentation();
        }

        private Size pictureSize;

        private void OnResizeBegin(object sender, EventArgs e)
        {
            pictureSize = diskBitmap.Size;

            if (Defragmenter != null)
            {
                Defragmenter.Pause();
            }

            diskBitmap.SetBusy(true);
        }

        private void OnResizeEnd(object sender, EventArgs e)
        {
            Size newPictureSize = diskBitmap.Size;

            diskBitmap.SetBusy(false);

            if (!pictureSize.Equals(newPictureSize))
            {
                diskBitmap.Initialize(GuiSettings);

                Defragmenter.StartReparseThread(diskBitmap.NumSquares);

                //if (Defragmenter != null)
                //    Defragmenter.NumFilteredClusters = diskBitmap.NumSquares;
            }

            if (Defragmenter != null)
                Defragmenter.Continue();
        }

        private void OnShow(object sender, EventArgs e)
        {
            diskBitmap.Initialize(GuiSettings);
        }

        #endregion

        #region Variables

        private IDefragmenter Defragmenter;

        private DefragmentationSettings DefragSettings;
        private GuiSettings GuiSettings;

        #endregion
    }
}
