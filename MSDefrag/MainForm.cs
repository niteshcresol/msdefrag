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
            Initialize();
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            InitializeComponent();

            GuiRefreshTimer = new System.Timers.Timer(1000);

            GuiRefreshTimer.Elapsed += new ElapsedEventHandler(OnRefreshGuiTimer);
            GuiRefreshTimer.Enabled = true;
        }

        private void InitializeBitmapDisplay()
        {
            diskBitmap = null;

            diskBitmap = new DiskBitmap(pictureBox1.Width, pictureBox1.Height, 10, m_defragmenter.NumClusters);
            pictureBox1.Image = diskBitmap.bitmap;
        }

        #endregion

        #region Graphics functions

        private void RefreshDisplay()
        {
            _inUse = true;

            if (diskBitmap != null)
            {
                diskBitmap.DrawAllMapSquares();
                pictureBox1.Invalidate();
            }

            _inUse = false;
        }

        private void AddChangedClustersToQueue(IList<MSDefragLib.ClusterState> changedClusters)
        {
            if (changedClusters == null || diskBitmap == null)
                return;

            diskBitmap.AddChangedClusters(changedClusters);
        }

        private void AddFilteredClustersToQueue(IList<MSDefragLib.MapClusterState> filteredClusters)
        {
            if (filteredClusters == null || diskBitmap == null)
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
                    m_defragmenter = DefragmenterFactory.Create();
                    break;
                default:
                    m_defragmenter = DefragmenterFactory.CreateSimulation();
                    break;
            }

            m_defragmenter.StartDefragmentation("A");

            m_defragmenter.ProgressEvent += new EventHandler<ProgressEventArgs>(UpdateProgress);
            //m_defragmenter.UpdateDiskMapEvent += new UpdateDiskMapHandler(UpdateDiskMap);
            m_defragmenter.UpdateFilteredDiskMapEvent += new EventHandler<FilteredClusterEventArgs>(UpdateFilteredDiskMap);

            InitializeBitmapDisplay();
        }

        private void StopDefragmentation()
        {
            m_defragmenter.StopDefragmentation(4000);

            m_defragmenter.ProgressEvent -= new EventHandler<ProgressEventArgs>(UpdateProgress);
            //m_defragmenter.UpdateDiskMapEvent -= new UpdateDiskMapHandler(UpdateDiskMap);
            m_defragmenter.UpdateFilteredDiskMapEvent -= new EventHandler<FilteredClusterEventArgs>(UpdateFilteredDiskMap);
        }

        private void UpdateProgressBar(Double val)
        {
            progressBar.Value = (Int16)val;

            progressBarText.Text = String.Format(CultureInfo.CurrentCulture, "{0:P}", val * 0.01);
        }

        private void ShowStatistics()
        {
            progressBarStatistics.Text = "Frame skip: " + _skippedFrames;
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

        private void UpdateDiskMap(object sender, EventArgs e)
        {
            ChangedClusterEventArgs ea = e as ChangedClusterEventArgs;

            if (ea != null)
            {
                AddChangedClustersToQueue(ea.Clusters);
            }
        }

        private void UpdateFilteredDiskMap(object sender, EventArgs e)
        {
            FilteredClusterEventArgs ea = e as FilteredClusterEventArgs;

            if (ea != null)
            {
                AddFilteredClustersToQueue(ea.Clusters);
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

        private void OnGuiClosing(object sender, FormClosingEventArgs e)
        {
            StopDefragmentation();
        }

        private void OnResizeBegin(object sender, EventArgs e)
        {
            ignoreEvent = true;
        }

        private void OnResizeEnd(object sender, EventArgs e)
        {
            ignoreEvent = false;
        }

        private bool _inUse;
        private int _skippedFrames;

        private void OnRefreshGuiTimer(object source, ElapsedEventArgs e)
        {
            if (ignoreEvent)
                return;

            if (_inUse == false)
            {
                RefreshDisplay();
            }
            else
            {
                _skippedFrames++;

                BeginInvoke(new MethodInvoker(delegate { ShowStatistics(); })); 
            }
        }

        #endregion

        #region Variables

        Boolean ignoreEvent;

        System.Timers.Timer GuiRefreshTimer;

        private IDefragmenter m_defragmenter;

        private enum EnumDefragType
        {
            defragTypeDefragmentation = 0,
            defragTypeSimulation
        }

        DiskBitmap diskBitmap;

        #endregion
    }
}
