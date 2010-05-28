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

            GuiSettings = new MSDefrag.GuiSettings(10);

            DefragSettings = new DefragmentationSettings();
            DefragSettings.Path = "C:\\*";
        }

        #endregion

        #region Initialization

        private void ResetBitmapDisplay()
        {
            diskBitmap.Initialize(GuiSettings);

            Defragmenter.SetNumFilteredClusters((UInt32)diskBitmap.NumSquares);
        }

        #endregion

        #region Graphics functions

        private void AddFilteredClustersToQueue(IList<MSDefragLib.MapClusterState> filteredClusters)
        {
            if (filteredClusters == null)
                return;

            diskBitmap.AddFilteredClusters(filteredClusters);
        }

        #endregion

        #region Other

        private void StartDefragmentation(EnumDefragType mode)
        {
            switch (mode)
            {
                case EnumDefragType.defragTypeDefragmentation:
                    Defragmenter = DefragmenterFactory.Create();
                    break;
                default:
                    Defragmenter = DefragmenterFactory.CreateSimulation();
                    break;
            }

            Defragmenter.StartDefragmentation("A");

            Defragmenter.ProgressEvent += new EventHandler<ProgressEventArgs>(UpdateProgress);
            Defragmenter.UpdateFilteredDiskMapEvent += new EventHandler<FilteredClusterEventArgs>(UpdateFilteredDiskMap);
            Defragmenter.LogMessageEvent += new EventHandler<LogMessagesEventArgs>(UpdateLogMessages);

            ResetBitmapDisplay();
        }

        private void StopDefragmentation()
        {
            if (Defragmenter == null)
            {
                return;
            }

            Defragmenter.StopDefragmentation(4000);

            Defragmenter.ProgressEvent -= new EventHandler<ProgressEventArgs>(UpdateProgress);
            Defragmenter.UpdateFilteredDiskMapEvent -= new EventHandler<FilteredClusterEventArgs>(UpdateFilteredDiskMap);
            Defragmenter.LogMessageEvent -= new EventHandler<LogMessagesEventArgs>(UpdateLogMessages);
        }

        private void UpdateProgressBar(Double val)
        {
            progressBar.Value = (Int16)val;

            progressBarText.Text = String.Format(CultureInfo.CurrentCulture, "{0:P}", val * 0.01);
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

            LogMessageLabel.Text = "<" + list.Last().LogLevel.ToString() + "> " + list.Last().Message;
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

            //Defragmenter.Pause();

            diskBitmap.SetBusy(true);

        }

        private void OnResizeEnd(object sender, EventArgs e)
        {
            Size newPictureSize = diskBitmap.Size;

            if (pictureSize.Height != newPictureSize.Height || pictureSize.Width != newPictureSize.Width)
            {
                diskBitmap.Initialize(GuiSettings);
                //Defragmenter.SetNumFilteredClusters((UInt32)diskBitmap.NumSquares);
                //Defragmenter.Continue();
            }

            diskBitmap.SetBusy(false);
        }

        #endregion

        #region Variables

        private IDefragmenter Defragmenter;

        private enum EnumDefragType
        {
            defragTypeDefragmentation = 0,
            defragTypeSimulation
        }

        private DefragmentationSettings DefragSettings;
        private GuiSettings GuiSettings;

        #endregion

        private void OnShow(object sender, EventArgs e)
        {
            diskBitmap.Initialize(GuiSettings);
        }

        private void OnDiskBitmapSizeChanged(object sender, EventArgs e)
        {
//            pictureBox1.Initialize(10);
        }
    }
}
