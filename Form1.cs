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
        MSDefragLib.MSDefragLib m_msDefragLib;

        public void Defrag()
        {
            m_msDefragLib.RunJkDefrag("C:\\*", 2, 100, 10, null, null);
        }

        String m_message;

        private const int maxMessages = 5;

        String[] messages = new String[maxMessages];

        Bitmap bmp = new Bitmap(1000, 500);
        Bitmap statusBmp = new Bitmap(1000, 300);
        
        Rectangle m_rec = new Rectangle(0, 300, 1000, 500);
        Rectangle m_rec2 = new Rectangle(0, 0, 1000, 500);
        
        Font m_font = new Font("Tahoma", 10);

        SolidBrush backBrush = new SolidBrush(Color.Blue);
        SolidBrush fontBrush = new SolidBrush(Color.Yellow);

        public Int32 maxSquare = 5000;

        // This will be called whenever the list changes.
        private void ShowChanges(object sender, EventArgs e)
        {
            m_message = m_msDefragLib.GetLastMessage();

            Int32 level = System.Convert.ToInt32(m_message.Substring(1, 1));

            AddStatusMessage(level, m_message.Substring(3));
        }

        private void AddStatusMessage(Int32 level, String message)
        {
            messages[level] = message;

            //for (int ii = 0; ii < messages.Length - 1; ii++)
            //{
            //    messages[ii] = messages[ii + 1];
            //}

            //messages[maxMessages - 1] = message;

            PaintStatus();
        }

        private void PaintStatus()
        {
            try
            {
                Graphics g = pictureBox2.CreateGraphics();

                Graphics g1 = Graphics.FromImage(statusBmp);

                g1.FillRectangle(backBrush, m_rec2);

                for (int ii = 0; ii < maxMessages; ii++)
                {
                    if (messages[ii] == null) continue;

                    String mess = messages[ii];

                    g1.DrawString(mess, m_font, fontBrush, new PointF(0, 15 * ii));
                }

                g.DrawImageUnscaled(statusBmp, 0, 0);
            }
            catch (System.Exception e)
            {
                AddStatusMessage(3, e.Message);
                m_msDefragLib.ShowDebugEvent -= new MSDefragLib.MSDefragLib.ShowDebugHandler(ShowChanges);
            }
        }

        List<MSDefragLib.MSDefragLib.colors> m_clusters = null;

        private void DrawCluster(object sender, EventArgs e)
        {
            if (e is DrawClusterEventArgs2)
            {
                DrawClusterEventArgs2 dcea = (DrawClusterEventArgs2)e;

//                m_clusters = m_msDefragLib.GetClusterList(dcea.m_data, maxSquare, (Int64)dcea.m_startClusterNumber, (Int64)dcea.m_endClusterNumber);

//                PaintClusters(dcea.m_data, (UInt64)dcea.m_startClusterNumber, (UInt64)dcea.m_endClusterNumber);
                DrawSquares(dcea.m_squareBegin, dcea.m_squareEnd);
            }
        }

        private static System.Timers.Timer aTimer;
        private static Form1 m_form;

        Thread defragThread = null;

        public Form1()
        {
            m_form = this;

            m_msDefragLib = new MSDefragLib.MSDefragLib();

            m_msDefragLib.ShowDebugEvent += new MSDefragLib.MSDefragLib.ShowDebugHandler(ShowChanges);
            m_msDefragLib.DrawClusterEvent += new MSDefragLib.MSDefragLib.DrawClusterHandler(DrawCluster);

            InitializeComponent();

            InitializeDiskMap();


            //Defrag();

            defragThread = new Thread(Defrag);

            defragThread.Start();

            aTimer = new System.Timers.Timer(500);

            aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);

            aTimer.Interval = 100;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            m_form.DrawSquares(0, m_form.m_numSquares);

            //if (m_form.defragThread.IsAlive == false)
            //{
            //    aTimer.Enabled = false;
            //}
        }

        private const Int32 m_squareSize = 6;

        Int32 m_numSquaresX = 1;
        Int32 m_numSquaresY = 1;

        private Int32 m_numSquares = 1;

        public void InitializeDiskMap()
        {
            m_numSquaresX = pictureBox1.Width / m_squareSize;
            m_numSquaresY = pictureBox1.Height / m_squareSize;

            m_numSquares = m_numSquaresX * m_numSquaresY;

            m_msDefragLib.NumSquares = m_numSquares;

            DrawSquares(0, m_numSquares);
        }

        private void DrawSquares(Int32 squareBegin, Int32 squareEnd)
        {
            List<MSDefragLib.ClusterSquare> squaresList = m_msDefragLib.GetSquareList(squareBegin, squareEnd);

            try
            {
                Graphics g = pictureBox1.CreateGraphics();

                Graphics g1 = Graphics.FromImage(bmp);

                AddStatusMessage(1, "Square: " + squareBegin);

                foreach (MSDefragLib.ClusterSquare square in squaresList)
                {
                    Int32 squareIndex = square.m_squareIndex;
                    MSDefragLib.MSDefragLib.colors color = square.m_color;

                    Int32 posX = (Int32)(squareIndex % m_numSquaresX);
                    Int32 posY = (Int32)(squareIndex / m_numSquaresX);

                    Rectangle rec = new Rectangle(posX * m_squareSize, posY * m_squareSize, m_squareSize - 1, m_squareSize - 1);

                    Color col = Color.White;

                    switch (color)
                    {
                        case MSDefragLib.MSDefragLib.colors.COLORUNMOVABLE:
                            col = Color.Red;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORALLOCATED:
                            col = Color.Yellow;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORBACK:
                            col = Color.White;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORBUSY:
                            col = Color.Blue;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLOREMPTY:
                            col = Color.White;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORFRAGMENTED:
                            col = Color.Orange;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORMFT:
                            col = Color.Pink;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORSPACEHOG:
                            col = Color.GreenYellow;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORUNFRAGMENTED:
                            col = Color.Green;
                            break;
                        default:
                            col = Color.White;
                            break;
                    }

                    SolidBrush brush = new SolidBrush(col);

                    g1.FillRectangle(brush, rec);
                }

                g.DrawImageUnscaled(bmp, 0, 0);

            }
            catch (System.Exception e)
            {
                AddStatusMessage(3, e.Message);
                m_msDefragLib.DrawClusterEvent -= new MSDefragLib.MSDefragLib.DrawClusterHandler(DrawCluster);
            }
        }

        private void PaintClusters(MSDefragDataStruct Data, UInt64 startClusterNumber, UInt64 endClusterNumber)
        {
            try
            {
                Graphics g = pictureBox1.CreateGraphics();

                Graphics g1 = Graphics.FromImage(bmp);

                Int32 numClustersX = pictureBox1.Width / 5;
                Int32 numClustersY = pictureBox1.Height / 5;

//                Int32 allClusters = numClustersX * numClustersY;
                Int32 allClusters = maxSquare;

                Double clusterPerSquare = Data.TotalClusters / (UInt64)allClusters;

                Int32 startSquare = (Int32)(startClusterNumber / clusterPerSquare);
                Int32 endSquare = (Int32)(endClusterNumber / clusterPerSquare);

                for (int ii = 0; ii <= endSquare - startSquare; ii++)
                {
                    Int32 clusterNumber = startSquare + ii;

                    Int32 posX = (Int32)(clusterNumber % numClustersX);
                    Int32 posY = (Int32)(clusterNumber / numClustersX);

                    Rectangle rec = new Rectangle(posX * 5, posY * 5, 4, 4);

                    Color col = Color.White;

                    MSDefragLib.MSDefragLib.colors color = m_clusters[ii];

                    switch (color)
                    {
                        case MSDefragLib.MSDefragLib.colors.COLORUNMOVABLE:
                            col = Color.Red;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORALLOCATED:
                            col = Color.Yellow;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORBACK:
                            col = Color.White;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORBUSY:
                            col = Color.Blue;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLOREMPTY:
                            col = Color.Gray;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORFRAGMENTED:
                            col = Color.Orange;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORMFT:
                            col = Color.Pink;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORSPACEHOG:
                            col = Color.GreenYellow;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORUNFRAGMENTED:
                            col = Color.Green;
                            break;
                        default:
                            col = Color.White;
                            break;
                    }
                    SolidBrush brush = new SolidBrush(col);

                    g1.FillRectangle(brush, rec);

                    String kkk = "Cluster: " + startClusterNumber + " - " + endClusterNumber;

                    AddStatusMessage(1, kkk);

                    g.DrawImageUnscaled(bmp, 0, 0);

                    PaintStatus();
                }
            }
            catch (System.Exception e)
            {
                AddStatusMessage(3, e.Message);
                m_msDefragLib.DrawClusterEvent -= new MSDefragLib.MSDefragLib.DrawClusterHandler(DrawCluster);
            }
        }

        private void PaintAllClusters(MSDefragDataStruct Data, UInt64 clusterNumber)
        {
            try
            {
                Graphics g = pictureBox1.CreateGraphics();

                Graphics g1 = Graphics.FromImage(bmp);

                Int32 numClustersX = pictureBox1.Width / 5;
                Int32 numClustersY = pictureBox1.Height / 5;

                Int32 allClusters = numClustersX * numClustersY;
                Double clusterPerSquare = Data.TotalClusters / (UInt64)allClusters;




//                for (int ii = 0; ii < m_clusters.Count; ii++)
                int ii = (Int32)(clusterNumber / clusterPerSquare);
                {
                    Int32 posX = (Int32)(ii % numClustersX);
                    Int32 posY = (Int32)(ii / numClustersX);

                    Rectangle rec = new Rectangle(posX * 5, posY * 5, 4, 4);

                    Color col = Color.White;

                    MSDefragLib.MSDefragLib.colors color = m_clusters[ii];

                    switch (color)
                    {
                        case MSDefragLib.MSDefragLib.colors.COLORUNMOVABLE:
                            col = Color.Red;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORALLOCATED:
                            col = Color.Yellow;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORBACK:
                            col = Color.White;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORBUSY:
                            col = Color.Blue;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLOREMPTY:
                            col = Color.Gray;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORFRAGMENTED:
                            col = Color.Orange;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORMFT:
                            col = Color.Pink;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORSPACEHOG:
                            col = Color.GreenYellow;
                            break;
                        case MSDefragLib.MSDefragLib.colors.COLORUNFRAGMENTED:
                            col = Color.Green;
                            break;
                        default:
                            col = Color.White;
                            break;
                    }
                    SolidBrush brush = new SolidBrush(col);

                    g1.FillRectangle(brush, rec);

                    String kkk = "Cluster: " + clusterNumber;

                    AddStatusMessage(1, kkk);

                    g.DrawImageUnscaled(bmp, 0, 0);

                    PaintStatus();
                }
            }
            catch (System.Exception)
            {
                m_msDefragLib.DrawClusterEvent -= new MSDefragLib.MSDefragLib.DrawClusterHandler(DrawCluster);
            }
        }
        private void PaintCluster(MSDefragDataStruct Data, UInt64 clusterNumber, MSDefragLib.MSDefragLib.colors color)
        {
            try
            {
                Graphics g = pictureBox1.CreateGraphics();

                Graphics g1 = Graphics.FromImage(bmp);

                Int32 numClustersX = pictureBox1.Width / 5;
                Int32 numClustersY = pictureBox1.Height / 5;

                Int32 allClusters = numClustersX * numClustersY;
                Int32 totalClusters = (Int32)Data.TotalClusters;

                Double clusterPerSquare = totalClusters / allClusters;

                Int32 posX = (Int32)((clusterNumber / clusterPerSquare) % numClustersX);
                Int32 posY = (Int32)((clusterNumber / clusterPerSquare) / numClustersX);

                Rectangle rec = new Rectangle(posX * 5, posY * 5, 4, 4);

                Color col = Color.White;

                switch (color)
                {
                    case MSDefragLib.MSDefragLib.colors.COLORUNMOVABLE:
                        col = Color.Red;
                        break;
                    case MSDefragLib.MSDefragLib.colors.COLORALLOCATED:
                        col = Color.Yellow;
                        break;
                    case MSDefragLib.MSDefragLib.colors.COLORBACK:
                        col = Color.White;
                        break;
                    case MSDefragLib.MSDefragLib.colors.COLORBUSY:
                        col = Color.Blue;
                        break;
                    case MSDefragLib.MSDefragLib.colors.COLOREMPTY:
                        col = Color.Gray;
                        break;
                    case MSDefragLib.MSDefragLib.colors.COLORFRAGMENTED:
                        col = Color.Orange;
                        break;
                    case MSDefragLib.MSDefragLib.colors.COLORMFT:
                        col = Color.Pink;
                        break;
                    case MSDefragLib.MSDefragLib.colors.COLORSPACEHOG:
                        col = Color.GreenYellow;
                        break;
                    case MSDefragLib.MSDefragLib.colors.COLORUNFRAGMENTED:
                        col = Color.Green;
                        break;
                    default:
                        col = Color.White;
                        break;
                }
                SolidBrush brush = new SolidBrush(col);

                g1.FillRectangle(brush, rec);

                String kkk = "Cluster: " + clusterNumber;

                AddStatusMessage(1, kkk);

                g.DrawImageUnscaled(bmp, 0, 0);

                PaintStatus();
            }
            catch (System.Exception)
            {
                m_msDefragLib.DrawClusterEvent -= new MSDefragLib.MSDefragLib.DrawClusterHandler(DrawCluster);
            }
        }
    }
}
