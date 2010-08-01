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
            Defragmenter = DefragmenterFactory.Create();

            diskBitmap.Defragmenter = Defragmenter;
            UpdateDiskBitmap();
        }

        #endregion

        #region Actions

        private void StartDefragmentation()
        {
            toolButtonStartDefrag.Enabled = false;
            toolButtonStopDefrag.Enabled = true;

            RegisterEvents(true);

            UpdateDiskBitmap();
            Defragmenter.StartDefragmentation(DefragSettings.Path);
        }

        private void StopDefragmentation()
        {
            toolButtonStartDefrag.Enabled = false;
            toolButtonStopDefrag.Enabled = false;

            Defragmenter.StopDefragmentation(4000);

            RegisterEvents(false);

            toolButtonStartDefrag.Enabled = true;
            toolButtonStopDefrag.Enabled = false;
        }


        private void StartResizing()
        {
            Defragmenter.Pause();

            diskBitmap.StartResizing();
        }

        private void StopResizing()
        {
            diskBitmap.StopResizing();

            Defragmenter.NumFilteredClusters = diskBitmap.NumSquares;

            Defragmenter.Continue();
        }

        private void UpdateDiskBitmap()
        {
            diskBitmap.Initialize(GuiSettings);
            Defragmenter.NumFilteredClusters = diskBitmap.NumSquares;
        }

        #endregion

        #region Defrag library events

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

        #region ProgressBar

        private void UpdateProgress(object sender, EventArgs e)
        {
            ProgressEventArgs ea = e as ProgressEventArgs;

            if (ea != null)
            {
                BeginInvoke(new MethodInvoker(delegate { UpdateProgressBar(ea.Progress); }));
            }
        }

        private void UpdateProgressBar(Double val)
        {
            progressBar.Value = (Int16)val;

            progressBarText.Text = String.Format(CultureInfo.InstalledUICulture, "{0:P4}", val * 0.01);
        }

        #endregion

        #region DiskMap

        private void UpdateFilteredDiskMap(object sender, EventArgs e)
        {
            FilteredClusterEventArgs ea = e as FilteredClusterEventArgs;

            if (ea != null)
            {
                diskBitmap.AddFilteredClusters(ea.Clusters);
            }
        }

        #endregion

        #region Log Messages

        private void UpdateLogMessages(object sender, EventArgs e)
        {
            LogMessagesEventArgs ea = e as LogMessagesEventArgs;

            if (ea != null)
            {
                BeginInvoke(new MethodInvoker(delegate { UpdateLogMessage(ea.Messages); }));
            }
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

        #endregion

        #region Event Handling

        private void OnStartDefrag(object sender, EventArgs e)
        {
            StartDefragmentation();
        }

        private void OnStopDefrag(object sender, EventArgs e)
        {
            StopDefragmentation();
        }

        private void OnGuiClosing(object sender, FormClosingEventArgs e)
        {
            StopDefragmentation();
        }

        private void OnResizeBegin(object sender, EventArgs e)
        {
            StartResizing();
        }

        private void OnResizeEnd(object sender, EventArgs e)
        {
            StopResizing();
        }

        #endregion

        #region Variables

        private IDefragmenter Defragmenter;
        private DefragmentationSettings DefragSettings;
        private GuiSettings GuiSettings;

        #endregion
    }
}
