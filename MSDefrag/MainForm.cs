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

namespace MSDefrag
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            m_this = this;

            Initialize();
        }

        #region Initialization

        public void Initialize()
        {
            m_defragmenter = new DefragmenterFactory();

            InitializeComponent();

            GuiRefreshTimer = new System.Timers.Timer(100);

            GuiRefreshTimer.Elapsed += new ElapsedEventHandler(OnRefreshGuiTimer);
            GuiRefreshTimer.Enabled = true;
        }

        private void InitializeBitmapDisplay()
        {
            diskBitmap = null;

            diskBitmap = new DiskBitmap(pictureBox1.Width, pictureBox1.Height, 10);

            pictureBox1.Image = diskBitmap.bitmap;
        }

        #endregion

        #region Graphics functions

        private void RefreshDisplay()
        {
            _inUse = true;
            try
            {
                pictureBox1.Invalidate();
            }
            finally
            {
                _inUse = false;
            }
        }

        Queue<IList<ClusterSquare>> queue = new Queue<IList<ClusterSquare>>();

        private void AddChangedClustersToQueue(IList<MSDefragLib.ClusterSquare> squaresList)
        {
            if (squaresList == null)
                return;

            lock (queue)
            {
                queue.Enqueue(squaresList);
            }
        }

        //private void DrawChangedClusters()
        //{
        //    lock (m_bitmapDisplay)
        //    {
        //        Queue<IList<ClusterSquare>> list;

        //        lock (queue)
        //        {
        //            list = queue;

        //            queue = new Queue<IList<ClusterSquare>>();
        //        }

        //        int numSquares = 0;

        //        foreach (IList<ClusterSquare> cs in list)
        //        {
        //            numSquares += cs.Count;

        //            foreach (MSDefragLib.ClusterSquare square in cs)
        //            {
        //                Int32 squareIndex = square.m_squareIndex;

        //                Int32 posX = (Int32)(squareIndex % m_numSquaresX);
        //                Int32 posY = (Int32)(squareIndex / m_numSquaresX);

        //                m_graphicsClusters.DrawImageUnscaled(squareBitmaps[(Int32)square.m_color], posX * m_squareSize + 1, posY * m_squareSize + 1);
        //            }
        //        }

        //        AddStatusMessage(3, "Draw squares: " + numSquares);
        //    }
        //}

        #endregion

        #region Other

        private void StartDefragmentation(m_eDefragType mode)
        {
            switch (mode)
            {
                case m_eDefragType.defragTypeDefragmentation:
                    m_defragmenter.Create();
                    break;
                case m_eDefragType.defragTypeSimulation:
                    m_defragmenter.CreateSimulation();
                    break;
                default:
                    m_defragmenter.CreateSimulation();
                    break;
            }

            m_defragmenter.Start();

            //m_defragmenter.ClustersModified += new MSDefragLib.ClustersModifiedHandler(ShowChangedClusters);
            m_defragmenter.defragmenter.Progress += new MSDefragLib.ProgressHandler(UpdateProgress);

            InitializeBitmapDisplay();
        }

        private void StopDefragmentation()
        {
            //m_defragmenter.ClustersModified -= new MSDefragLib.ClustersModifiedHandler(ShowChangedClusters);

            m_defragmenter.Stop(4000);
        }

        private void UpdateProgressBar(Double val)
        {
            progressBar.Value = (Int16)val;

            progressBarText.Text = String.Format("{0:P}", val * 0.01);
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

            StartDefragmentation(m_eDefragType.defragTypeDefragmentation);
        }

        private void OnStartSimulation(object sender, EventArgs e)
        {
            toolButtonStartDefrag.Enabled = false;
            toolButtonStartSimulation.Enabled = false;
            toolButtonStopDefrag.Enabled = true;

            StartDefragmentation(m_eDefragType.defragTypeSimulation);
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

        private void ShowChangedClusters(object sender, EventArgs e)
        {
            if (ignoreEvent)
                return;

            if (e is ChangedClusterEventArgs)
            {
                ChangedClusterEventArgs ea = (ChangedClusterEventArgs)e;

                AddChangedClustersToQueue(ea.m_list);
            }
        }

        private void UpdateProgress(object sender, EventArgs e)
        {
            if (e is ProgressEventArgs)
            {
                ProgressEventArgs ea = (ProgressEventArgs)e;

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

        private bool _inUse = false;
        private int _skippedFrames = 0;

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

        public static MainForm m_this;

        private DefragmenterFactory m_defragmenter = null;

        public enum m_eDefragType
        {
            defragTypeDefragmentation = 0,
            defragTypeSimulation
        }

        DiskBitmap diskBitmap;

        #endregion
    }
}
