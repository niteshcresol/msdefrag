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
        #region Constructor

        public MainForm()
        {
            Initialize();

            m_this = this;

            GuiRefreshTimer = new System.Timers.Timer(100);
            GuiRefreshTimer.Elapsed += new ElapsedEventHandler(OnRefreshGuiTimer);

            GuiRefreshTimer.Enabled = true;

            GetSquareTimer = new System.Timers.Timer(30);
            GetSquareTimer.Elapsed += new ElapsedEventHandler(OnGetSquaresTimer);

            GetSquareTimer.Enabled = true;
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            InitializeComponent();
        }

        private void InitializeBitmapClusters()
        {
            m_squareSize = 12;
            m_realSquareSize = m_squareSize > 1 ? m_squareSize - 1 : 1;

            m_numSquaresX = (pictureBox1.Width - 2) / m_squareSize;
            m_numSquaresY = (pictureBox1.Height - 2) / m_squareSize;

            m_bitmapClusters = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            m_numSquares = m_numSquaresX * m_numSquaresY;
            m_defragmenter.NumSquares = m_numSquares;

            m_graphicsClusters = Graphics.FromImage(m_bitmapClusters);

            m_graphicsClusters.DrawRectangle(Pens.Blue, 0, 0, m_numSquaresX * m_squareSize + 1, m_numSquaresY * m_squareSize + 1);
        }

        private void InitializeBitmapStatus()
        {
            m_font = new Font("Tahoma", 10);

            maxMessages = 7;
            messages = new String[maxMessages];

            m_rectangleStatusBitmap = new Rectangle(40, 30, pictureBox1.Width - 80, 170);

            m_bitmapStatus = new Bitmap(m_rectangleStatusBitmap.Width, m_rectangleStatusBitmap.Height);

            m_graphicsStatus = Graphics.FromImage(m_bitmapStatus);
            m_brushStatusBackground = new SolidBrush(Color.FromArgb(210, Color.LightBlue));
        }

        private void InitializeBitmapDisplay()
        {
            m_bitmapDisplay = new Bitmap(pictureBox1.Width, pictureBox1.Height);

            pictureBox1.Image = m_bitmapDisplay;

            m_graphicsDisplay = Graphics.FromImage(m_bitmapDisplay);
        }

        private void InitBrushes()
        {
            backBrush = new SolidBrush(Color.Blue);
            fontBrush = new SolidBrush(Color.Yellow);

            colors = new Color[(Int32)MSDefragLib.eClusterState.MaxValue];

            colors[(Int32)MSDefragLib.eClusterState.Unmovable] = Color.Yellow;
            colors[(Int32)MSDefragLib.eClusterState.Allocated] = Color.LightGray;
            colors[(Int32)MSDefragLib.eClusterState.Busy] = Color.Blue;
            colors[(Int32)MSDefragLib.eClusterState.Free] = Color.White;
            colors[(Int32)MSDefragLib.eClusterState.Fragmented] = Color.Orange;
            colors[(Int32)MSDefragLib.eClusterState.Mft] = Color.Pink;
            colors[(Int32)MSDefragLib.eClusterState.SpaceHog] = Color.DarkCyan;
            colors[(Int32)MSDefragLib.eClusterState.Unfragmented] = Color.Green;

            brushes = new SolidBrush[(Int32)MSDefragLib.eClusterState.MaxValue];

            int ii = 0;

            foreach (Color col in colors)
            {
                brushes[ii] = new SolidBrush(col);
            }

            gradientBrushes = new LinearGradientBrush[(Int32)MSDefragLib.eClusterState.MaxValue];

            Byte brightnessFactor = 20;
            Byte darknessFactor = 70;

            ii = 0;

            foreach (Color col in colors)
            {
                Byte r = (Byte)(((col.R - darknessFactor) > 0) ? (col.R - darknessFactor) : 0);
                Byte g = (Byte)(((col.G - darknessFactor) > 0) ? (col.G - darknessFactor) : 0);
                Byte b = (Byte)(((col.B - darknessFactor) > 0) ? (col.B - darknessFactor) : 0);

                Byte r2 = (Byte)(((col.R + brightnessFactor) < Byte.MaxValue) ? (col.R + brightnessFactor) : Byte.MaxValue);
                Byte g2 = (Byte)(((col.G + brightnessFactor) < Byte.MaxValue) ? (col.G + brightnessFactor) : Byte.MaxValue);
                Byte b2 = (Byte)(((col.B + brightnessFactor) < Byte.MaxValue) ? (col.B + brightnessFactor) : Byte.MaxValue);

                Color darkColor = Color.FromArgb(r, g, b);
                Color brightColor = Color.FromArgb(r2, g2, b2);

                gradientBrushes[ii] = new LinearGradientBrush(new Rectangle(-1, -1, m_squareSize, m_squareSize), brightColor,
                    darkColor, LinearGradientMode.Horizontal);

                ii++;
            }

            verticalBrushes = new LinearGradientBrush[(Int32)MSDefragLib.eClusterState.MaxValue];

            brightnessFactor = 0;
            darknessFactor = 100;

            ii = 0;

            foreach (Color col in colors)
            {
                Byte r = (Byte)(((col.R - darknessFactor) > 0) ? (col.R - darknessFactor) : 0);
                Byte g = (Byte)(((col.G - darknessFactor) > 0) ? (col.G - darknessFactor) : 0);
                Byte b = (Byte)(((col.B - darknessFactor) > 0) ? (col.B - darknessFactor) : 0);

                Byte r2 = (Byte)(((col.R + brightnessFactor) < Byte.MaxValue) ? (col.R + brightnessFactor) : Byte.MaxValue);
                Byte g2 = (Byte)(((col.G + brightnessFactor) < Byte.MaxValue) ? (col.G + brightnessFactor) : Byte.MaxValue);
                Byte b2 = (Byte)(((col.B + brightnessFactor) < Byte.MaxValue) ? (col.B + brightnessFactor) : Byte.MaxValue);

                Color darkColor = Color.FromArgb(r, g, b);
                Color brightColor = Color.FromArgb(r2, g2, b2);

                verticalBrushes[ii] = new LinearGradientBrush(new Rectangle(-1, -1, m_squareSize, m_squareSize), darkColor,
                    brightColor, LinearGradientMode.Vertical);
                //gradientBrushes[ii] = new LinearGradientBrush(new Rectangle(1 - m_squareSize % 2, 1 - m_squareSize % 2, pictureBox1.Width, pictureBox1.Height), brightColor,
                //    darkColor, LinearGradientMode.ForwardDiagonal);

                ii++;
            }
        }

        private void InitSquareRectangles()
        {
            squareBitmaps = new Bitmap[(Int32)MSDefragLib.eClusterState.MaxValue];

            int ii = 0;

            foreach (Color col in colors)
            {
                Byte brightnessFactor = 80;
                Byte darknessFactor = 70;

                Byte r = (Byte)Math.Max(0, col.R - darknessFactor);
                Byte g = (Byte)Math.Max(0, col.G - darknessFactor);
                Byte b = (Byte)Math.Max(0, col.B - darknessFactor);

                Byte r2 = (Byte)Math.Min(Byte.MaxValue, col.R + brightnessFactor);
                Byte g2 = (Byte)Math.Min(Byte.MaxValue, col.G + brightnessFactor);
                Byte b2 = (Byte)Math.Min(Byte.MaxValue, col.B + brightnessFactor);

                Color darkColor = Color.FromArgb(r, g, b);
                Color brightColor = Color.FromArgb(r2, g2, b2);

                squareBitmaps[(Int32)ii] = new Bitmap(m_realSquareSize + 1, m_realSquareSize + 1);

                using (Graphics g1 = Graphics.FromImage(squareBitmaps[(Int32)ii]))
                {
                    Rectangle rec = new Rectangle(0, 0, m_squareSize + 1, m_squareSize + 1);

                    using (LinearGradientBrush br = new LinearGradientBrush(rec, brightColor,
                        darkColor, LinearGradientMode.ForwardDiagonal))
                    {

                        if (ii == (Int32)MSDefragLib.eClusterState.Free)
                        {
                            g1.FillRectangle(verticalBrushes[(Int32)ii], rec);
                        }
                        else
                        {
                            g1.FillRectangle(br, rec);
                        }
                    }
                }

                ii++;
            }
        }

        #endregion

        #region Graphics functions

        private void RefreshDisplay()
        {
            _inUse = true;
            try
            {
                lock (m_bitmapDisplay)
                {
                    DrawChangedClusters();
                    PaintStatus();

                    m_graphicsDisplay.DrawImageUnscaled(m_bitmapClusters, 0, 0);
                    m_graphicsDisplay.DrawImageUnscaled(m_bitmapStatus, m_rectangleStatusBitmap);

                    pictureBox1.Invalidate();
                }
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

                AddStatusMessage(2, "Num received squares:" + squaresList.Count);
            }
        }

        private void DrawChangedClusters()
        {
            lock (m_bitmapDisplay)
            {
                Queue<IList<ClusterSquare>> list;

                lock (queue)
                {
                    list = queue;

                    queue = new Queue<IList<ClusterSquare>>();
                }

                int numSquares = 0;

                foreach (IList<ClusterSquare> cs in list)
                {
                    numSquares += cs.Count;

                    foreach (MSDefragLib.ClusterSquare square in cs)
                    {
                        Int32 squareIndex = square.m_squareIndex;

                        Int32 posX = (Int32)(squareIndex % m_numSquaresX);
                        Int32 posY = (Int32)(squareIndex / m_numSquaresX);

                        m_graphicsClusters.DrawImageUnscaled(squareBitmaps[(Int32)square.m_color], posX * m_squareSize + 1, posY * m_squareSize + 1);
                    }
                }

                AddStatusMessage(3, "Draw squares: " + numSquares);
            }
        }

        private void PaintStatus()
        {
            lock (m_bitmapDisplay)
            {
                m_graphicsStatus.Clear(Color.Transparent);

                m_graphicsStatus.FillRectangle(m_brushStatusBackground, 0, 0, m_bitmapStatus.Width, m_bitmapStatus.Height);
                m_graphicsStatus.DrawRectangle(Pens.Black, 0, 0, m_bitmapStatus.Width - 1, m_bitmapStatus.Height - 1);

                lock (messages)
                {
                    for (int ii = 0; ii < maxMessages; ii++)
                        m_graphicsStatus.DrawString(messages[ii], m_font, Brushes.Black, 25, 25 + 15 * ii);
                }
            }
        }

        #endregion

        #region Other

        private void AddStatusMessage(UInt32 level, String message)
        {
            lock (messages)
            {
                messages[level] = message;
            }
        }

        private void StartDefragmentation(m_eDefragType mode)
        {
            switch (mode)
            {
                case m_eDefragType.defragTypeDefragmentation:
                    m_defragmenter = DefragmenterFactory.Create();
                    break;
                case m_eDefragType.defragTypeSimulation:
                    m_defragmenter = DefragmenterFactory.CreateSimulation();
                    break;
                default:
                    m_defragmenter = DefragmenterFactory.CreateSimulation();
                    break;
            }

            defragThread = new Thread(Defrag);
            defragThread.Priority = ThreadPriority.Lowest;

            m_defragmenter.NewMessage += new MSDefragLib.NewMessageHandler(SetStatus);
            m_defragmenter.ClustersModified += new MSDefragLib.ClustersModifiedHandler(ShowChangedClusters);
            m_defragmenter.Progress += new MSDefragLib.ProgressHandler(UpdateProgress);

            InitializeBitmapDisplay();
            InitializeBitmapClusters();
            InitializeBitmapStatus();

            InitBrushes();
            InitSquareRectangles();

            defragThread.Start();
        }

        private void Defrag()
        {
            m_defragmenter.Start(@"C:\*");
        }

        private void StopDefragmentation()
        {
            m_defragmenter.NewMessage -= new MSDefragLib.NewMessageHandler(SetStatus);
            m_defragmenter.ClustersModified -= new MSDefragLib.ClustersModifiedHandler(ShowChangedClusters);

            m_defragmenter.Stop(5000);

            if (defragThread.IsAlive)
            {
                try
                {
                    defragThread.Abort();
                }
                catch (System.Exception)
                {

                }

                while (defragThread.IsAlive)
                {
                    Thread.Sleep(1000);
                }
            }
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

                Double[] arg = new double[2];

                arg[0] = ea.Progress;

                BeginInvoke(new MethodInvoker(delegate { UpdateProgressBar(ea.Progress); }));
            }
        }

        // This will be called whenever the list changes.
        private void SetStatus(object sender, EventArgs e)
        {
            if (ignoreEvent)
                return;

            if (e is MSDefragLib.FileSystem.Ntfs.MSScanNtfsEventArgs)
            {
                MSDefragLib.FileSystem.Ntfs.MSScanNtfsEventArgs ev = (MSDefragLib.FileSystem.Ntfs.MSScanNtfsEventArgs)e;

                AddStatusMessage(ev.m_level, ev.m_message);
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

                ShowStatistics();
            }
        }

        private void OnGetSquaresTimer(object source, ElapsedEventArgs e)
        {
            AddChangedClustersToQueue(m_defragmenter.DirtySquares);
        }

        #endregion

        #region Graphic variables

        private int maxMessages;
        private Int32 m_squareSize;
        private Int32 m_realSquareSize;

        private String[] messages;

        private Font m_font;

        private SolidBrush backBrush;
        private SolidBrush fontBrush;
        private SolidBrush[] brushes;
        private LinearGradientBrush[] gradientBrushes;
        private LinearGradientBrush[] verticalBrushes;

        private Color[] colors;

        private Int32 m_numSquaresX;
        private Int32 m_numSquaresY;
        private Int32 m_numSquares;

        private Boolean ignoreEvent = false;

        private Bitmap m_bitmapClusters;
        private Bitmap m_bitmapStatus;
        private Bitmap m_bitmapDisplay;

        private Rectangle m_rectangleStatusBitmap;

        Graphics m_graphicsStatus;
        Graphics m_graphicsClusters;
        Graphics m_graphicsDisplay;

        SolidBrush m_brushStatusBackground;

        private Bitmap[] squareBitmaps;

        #endregion

        #region Other variables

        System.Timers.Timer GuiRefreshTimer;
        System.Timers.Timer GetSquareTimer;

        public static MainForm m_this;

        private Thread defragThread = null;

        private IDefragmenter m_defragmenter = null;

        public enum m_eDefragType
        {
            defragTypeDefragmentation = 0,
            defragTypeSimulation
        }

        #endregion

    }
}
