using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MSDefragLib;
using System.Threading;
using System.Timers;

namespace MSDefrag
{
    public partial class Form1 : Form
    {
        public void Defrag()
        {
            m_msDefragLib.RunJkDefrag("C:\\*", 2, 100, 10, null, null);
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

        private void AddStatusMessage(UInt32 level, String message)
        {
            messages[level] = message;

            PaintStatus();
        }

        //TODO: use standard controls, do not paint ourselves (unperformant)
        private void PaintStatus()
        {
            Graphics g1 = Graphics.FromImage(statusBmp);

            g1.FillRectangle(backBrush, m_rec2);

            for (int ii = 0; ii < maxMessages; ii++)
            {
                if (messages[ii] == null) continue;

                String mess = messages[ii];

                g1.DrawString(mess, m_font, fontBrush, new PointF(0, 15 * ii));
            }
            pictureBox2.Refresh();
        }

        private void DrawCluster(object sender, EventArgs e)
        {
            if (e is DrawClusterEventArgs2)
            {
                DrawClusterEventArgs2 dcea = (DrawClusterEventArgs2)e;

                DrawSquares(dcea.m_squareBegin, dcea.m_squareEnd);
            }
        }

        private void Notified(object sender, EventArgs e)
        {
            if (e is NotifyGuiEventArgs)
            {
                NotifyGuiEventArgs ngea = (NotifyGuiEventArgs)e;

//                DrawDirtySquare(ngea.m_clusterSquare);

                BeginInvoke(new MethodInvoker(delegate { DrawSquares(0,0); }));
            }
        }

        public Form1()
        {
            m_form = this;

            m_msDefragLib = new MSDefragLib.MSDefragLib();

            m_msDefragLib.ShowDebugEvent += new MSDefragLib.MSDefragLib.ShowDebugHandler(ShowChanges);
            m_msDefragLib.DrawClusterEvent += new MSDefragLib.MSDefragLib.DrawClusterHandler(DrawCluster);
            m_msDefragLib.NotifyGuiEvent += new MSDefragLib.MSDefragLib.NotifyGuiHandler(Notified);

            InitializeComponent();

            InitializeDiskMap();


            //Defrag();

            defragThread = new Thread(Defrag);

            defragThread.Start();

            //aTimer = new System.Timers.Timer(500);

            //aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            //aTimer.Interval = 1000;
            //aTimer.Enabled = true;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //m_form.DrawSquares(0, m_form.m_numSquares);
            //m_form.PaintStatus();
        }

        public void InitializeDiskMap()
        {
            m_numSquaresX = pictureBox1.Width / m_squareSize;
            m_numSquaresY = pictureBox1.Height / m_squareSize;

            m_numSquares = m_numSquaresX * m_numSquaresY;

            m_msDefragLib.NumSquares = m_numSquares;

            pictureBox1.Image = bmp;
            pictureBox2.Image = statusBmp;
            DrawSquares(0, m_numSquares);
        }

        private void DrawSquares(Int32 squareBegin, Int32 squareEnd)
        {
            IList<MSDefragLib.ClusterSquare> squaresList = m_msDefragLib.DirtySquares;

            if (squaresList == null)
                return;

            Graphics g1 = Graphics.FromImage(bmp);

            foreach (MSDefragLib.ClusterSquare square in squaresList)
            {
                Int32 squareIndex = square.m_squareIndex;
                MSDefragLib.MSDefragLib.CLUSTER_COLORS color = square.m_color;

                Int32 posX = (Int32)(squareIndex % m_numSquaresX);
                Int32 posY = (Int32)(squareIndex / m_numSquaresX);

                //                    AddStatusMessage(1, "Square: " + squareIndex);

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
            }
            pictureBox1.Refresh();
        }

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

//                AddStatusMessage(1, "Square: " + squareIndex);

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

        private static System.Timers.Timer aTimer;
        private static Form1 m_form;

        Thread defragThread = null;

        MSDefragLib.MSDefragLib m_msDefragLib = null;

        private const int maxMessages = 7;

        String[] messages = new String[maxMessages];

        Bitmap bmp = new Bitmap(1000, 900);
        Bitmap statusBmp = new Bitmap(1000, 800);

        Rectangle m_rec = new Rectangle(0, 300, 1000, 900);
        Rectangle m_rec2 = new Rectangle(0, 0, 1000, 800);

        Font m_font = new Font("Tahoma", 10);

        SolidBrush backBrush = new SolidBrush(Color.Blue);
        SolidBrush fontBrush = new SolidBrush(Color.Yellow);

        public Int32 maxSquare = 5000;

        private const Int32 m_squareSize = 7;

        Int32 m_numSquaresX = 1;
        Int32 m_numSquaresY = 1;

        private Int32 m_numSquares = 1;

        private void OnGuiClosing(object sender, FormClosingEventArgs e)
        {
            m_msDefragLib.StopJkDefrag(1000);

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

    }
}
