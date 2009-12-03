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
using MSDefragLib.FileSystem.Ntfs;

namespace MSDefrag
{
    public partial class MainForm : Form
    {
        #region Constructor

        public MainForm()
        {
            m_msDefragLib = new MSDefragLib.MSDefragLib();

            Initialize();

            defragThread = new Thread(Defrag);
            defragThread.Priority = ThreadPriority.Normal;

            defragThread.Start();
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            m_msDefragLib.ShowDebugEvent += new MSDefragLib.MSDefragLib.ShowDebugHandler(ShowChanges);
            m_msDefragLib.DrawClusterEvent += new MSDefragLib.MSDefragLib.DrawClusterHandler(DrawCluster);
            m_msDefragLib.NotifyGuiEvent += new MSDefragLib.MSDefragLib.NotifyGuiHandler(Notified);

            InitializeComponent();

            InitializeStatusPanel();
            InitializeDiskMap();

            InitBrushes();

        }

        public void InitializeStatusPanel()
        {
            statusBmp = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            pictureBox2.Image = statusBmp;

            m_font = new Font("Tahoma", 10);
            m_rec2 = new Rectangle(0, 0, pictureBox2.Width, pictureBox2.Height);

            maxMessages = 7;
            messages = new String[maxMessages];
        }

        public void InitializeDiskMap()
        {
            m_squareSize = 15;
            m_realSquareSize = m_squareSize > 1 ? m_squareSize - 1 : 1;

            m_numSquaresX = pictureBox1.Width / m_squareSize;
            m_numSquaresY = pictureBox1.Height / m_squareSize;

            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
            pictureBox1.Image = bmp;

            m_numSquares = m_numSquaresX * m_numSquaresY;
            m_msDefragLib.NumSquares = m_numSquares;

            DrawSquares();
        }

        private void InitBrushes()
        {
            backBrush = new SolidBrush(Color.Blue);
            fontBrush = new SolidBrush(Color.Yellow);

            colors = new Color[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORMAX];

            colors[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORUNMOVABLE] = Color.Yellow;
            colors[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORALLOCATED] = Color.LightGray;
            colors[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORBACK] = Color.White;
            colors[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORBUSY] = Color.Blue;
            colors[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLOREMPTY] = Color.White;
            colors[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORFRAGMENTED] = Color.Orange;
            colors[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORMFT] = Color.Pink;
            colors[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORSPACEHOG] = Color.GreenYellow;
            colors[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORUNFRAGMENTED] = Color.Green;

            brushes = new SolidBrush[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORMAX];

            int ii = 0;

            foreach (Color col in colors)
            {
                brushes[ii] = new SolidBrush(col);
            }

            gradientBrushes = new LinearGradientBrush[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORMAX];

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

            verticalBrushes = new LinearGradientBrush[(Int32)MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORMAX];

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

        #endregion

        #region Graphics functions

        private void DrawSquares()
        {
            IList<MSDefragLib.ClusterSquare> squaresList = m_msDefragLib.DirtySquares;

            if (squaresList == null)
                return;

            using (Graphics g1 = Graphics.FromImage(bmp))
            {
                foreach (MSDefragLib.ClusterSquare square in squaresList)
                {
                    Int32 squareIndex = square.m_squareIndex;

                    Int32 posX = (Int32)(squareIndex % m_numSquaresX);
                    Int32 posY = (Int32)(squareIndex / m_numSquaresX);

                    Rectangle rec = new Rectangle(posX * m_squareSize, posY * m_squareSize, m_realSquareSize + 1, m_realSquareSize + 1);
                    Rectangle rec2 = new Rectangle(posX * m_squareSize - 1, posY * m_squareSize - 1, m_squareSize + 1, m_squareSize + 1);

                    Color col = colors[(Int32)square.m_color];

                    Byte brightnessFactor = 80;
                    Byte darknessFactor = 70;

                    Byte r = (Byte)(((col.R - darknessFactor) > 0) ? (col.R - darknessFactor) : 0);
                    Byte g = (Byte)(((col.G - darknessFactor) > 0) ? (col.G - darknessFactor) : 0);
                    Byte b = (Byte)(((col.B - darknessFactor) > 0) ? (col.B - darknessFactor) : 0);

                    Byte r2 = (Byte)(((col.R + brightnessFactor) < Byte.MaxValue) ? (col.R + brightnessFactor) : Byte.MaxValue);
                    Byte g2 = (Byte)(((col.G + brightnessFactor) < Byte.MaxValue) ? (col.G + brightnessFactor) : Byte.MaxValue);
                    Byte b2 = (Byte)(((col.B + brightnessFactor) < Byte.MaxValue) ? (col.B + brightnessFactor) : Byte.MaxValue);

                    Color darkColor = Color.FromArgb(r, g, b);
                    Color brightColor = Color.FromArgb(r2, g2, b2);

                    using (LinearGradientBrush br = new LinearGradientBrush(rec, brightColor,
                        darkColor, LinearGradientMode.ForwardDiagonal))
                    {

                        if (square.m_color == MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLOREMPTY)
                        {
                            g1.FillRectangle(verticalBrushes[(Int32)square.m_color], rec);
                        }
                        else
                        {
                            g1.FillRectangle(br, rec);
                        }
                        //g1.FillRectangle(gradientBrushes[(Int32)square.m_color], rec);
                        //g1.FillRectangle(brushes[(Int32)square.m_color], rec);
                    }

                }
            }

            pictureBox1.Refresh();
        }

        //TODO: use standard controls, do not paint ourselves (unperformant)
        private void PaintStatus()
        {
            using (Graphics g1 = Graphics.FromImage(statusBmp))
            {
                g1.FillRectangle(backBrush, m_rec2);

                for (int ii = 0; ii < maxMessages; ii++)
                {
                    if (messages[ii] == null) continue;

                    String mess = messages[ii];

                    g1.DrawString(mess, m_font, fontBrush, new PointF(0, 15 * ii));
                }
            }
            pictureBox2.Refresh();
        }

        #region Unused

        private void DrawDirtySquare(ClusterSquare square)
        {
            if (square == null)
            {
                return;
            }
            Graphics g1 = Graphics.FromImage(bmp);

            Int32 squareIndex = square.m_squareIndex;
            MSDefragLib.MSDefragLib.CLUSTER_COLORS color = square.m_color;

            Int32 posX = (Int32)(squareIndex % m_numSquaresX);
            Int32 posY = (Int32)(squareIndex / m_numSquaresX);

            Rectangle rec = new Rectangle(posX * m_squareSize, posY * m_squareSize, m_squareSize - 1, m_squareSize - 1);

            Color col = Color.White;

            switch (color)
            {
                case MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORUNMOVABLE:
                    col = Color.Red;
                    break;
                case MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORALLOCATED:
                    col = Color.Yellow;
                    break;
                case MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORBACK:
                    col = Color.White;
                    break;
                case MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORBUSY:
                    col = Color.Blue;
                    break;
                case MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLOREMPTY:
                    col = Color.White;
                    break;
                case MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORFRAGMENTED:
                    col = Color.Orange;
                    break;
                case MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORMFT:
                    col = Color.Pink;
                    break;
                case MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORSPACEHOG:
                    col = Color.GreenYellow;
                    break;
                case MSDefragLib.MSDefragLib.CLUSTER_COLORS.COLORUNFRAGMENTED:
                    col = Color.Green;
                    break;
                default:
                    col = Color.White;
                    break;
            }

            SolidBrush brush = new SolidBrush(col);
            g1.FillRectangle(brush, rec);

            pictureBox1.Refresh();
        }

        #endregion

        #endregion

        #region Other

        private void AddStatusMessage(UInt32 level, String message)
        {
            messages[level] = message;

            PaintStatus();
        }

        private void Defrag()
        {
            //m_msDefragLib.Simulate();
            m_msDefragLib.RunJkDefrag("C:\\*", 2, 100, 10, null, null);
        }

        #endregion

        #region Event Handling

        private void Notified(object sender, EventArgs e)
        {
            if (e is NotifyGuiEventArgs)
            {
                NotifyGuiEventArgs ngea = (NotifyGuiEventArgs)e;

                BeginInvoke(new MethodInvoker(delegate { DrawSquares(); }));
            }
        }

        // This will be called whenever the list changes.
        private void ShowChanges(object sender, EventArgs e)
        {
            String message = "";
            UInt32 level = 0;

            if (e is MSScanNtfsEventArgs)
            {
                MSScanNtfsEventArgs ev = (MSScanNtfsEventArgs)e;
                message = ev.m_message;
                level = ev.m_level;
            }

            BeginInvoke(new MethodInvoker(delegate { AddStatusMessage(level, message); }));
        }

        private void OnGuiClosing(object sender, FormClosingEventArgs e)
        {
            if ((m_msDefragLib.m_data != null) && (m_msDefragLib.m_data.Running == RunningState.RUNNING))
            {
                m_msDefragLib.StopJkDefrag(5000);
            }

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

        #region Unused

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //m_form.DrawSquares(0, m_form.m_numSquares);
            //m_form.PaintStatus();
        }

        private void DrawCluster(object sender, EventArgs e)
        {
            if (e is DrawClusterEventArgs2)
            {
                DrawClusterEventArgs2 dcea = (DrawClusterEventArgs2)e;

                DrawSquares();
            }
        }

        #endregion

        #endregion

        #region Graphic variables

        private int maxMessages;
        private Int32 m_squareSize;
        private Int32 m_realSquareSize;

        private String[] messages;

        private Bitmap bmp;
        private Bitmap statusBmp;

        private Rectangle m_rec2;

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

        #endregion

        #region Other variables

        Thread defragThread = null;

        MSDefragLib.MSDefragLib m_msDefragLib = null;

        #endregion

    }
}
