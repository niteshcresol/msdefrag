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

namespace MSDefrag
{
    public partial class Form1 : Form
    {
        MSDefragLib.MSDefragLib m_msDefragLib;

        public void Defrag()
        {
            m_msDefragLib.RunJkDefrag("C:\\*", 1, 100, 10, null, null);
        }

        String m_message;

        private const int maxMessages = 20;

        String[] messages = new String[maxMessages];

        Bitmap bmp = new Bitmap(1000, 500);
        
        Rectangle m_rec = new Rectangle(0, 0, 1000, 500);
        
        Font m_font = new Font("Tahoma", 10);

        SolidBrush backBrush = new SolidBrush(Color.Blue);
        SolidBrush fontBrush = new SolidBrush(Color.Yellow);

        // This will be called whenever the list changes.
        private void ShowChanges(object sender, EventArgs e)
        {
            m_message = m_msDefragLib.GetLastMessage();

            for (int ii = 0; ii < messages.Length - 1; ii++ )
            {
                messages[ii] = messages[ii + 1];
            }

            messages[maxMessages - 1] = m_message;

            try
            {
                Graphics g = pictureBox1.CreateGraphics();

                Graphics g1 = Graphics.FromImage(bmp);

                //g1.FillRectangle(backBrush, m_rec);

                //for (int ii = 0; ii < maxMessages; ii++)
                //{
                //    if (messages[ii] == null) continue;

                //    String mess = messages[ii];

                //    g1.DrawString(mess, m_font, fontBrush, new PointF(15, 15 * ii));
                //}

                g.DrawImageUnscaled(bmp, 0, 0);
            }
            catch (System.Exception)
            {
                m_msDefragLib.ShowDebugEvent -= new MSDefragLib.MSDefragLib.ShowDebugHandler(ShowChanges);
            }
        }

        private void DrawCluster(object sender, EventArgs e)
        {
            if (e is DrawClusterEventArgs)
            {
                DrawClusterEventArgs dcea = (DrawClusterEventArgs)e;

                PaintCluster(dcea.m_data, dcea.m_clusterNumber, dcea.m_color);
            }
        }

        public Form1()
        {
            m_msDefragLib = new MSDefragLib.MSDefragLib();

            m_msDefragLib.ShowDebugEvent += new MSDefragLib.MSDefragLib.ShowDebugHandler(ShowChanges);
            m_msDefragLib.DrawClusterEvent += new MSDefragLib.MSDefragLib.DrawClusterHandler(DrawCluster);

            InitializeComponent();

            //Defrag();

            Thread defragThread = new Thread(Defrag);

            defragThread.Start();
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

                g.DrawImageUnscaled(bmp, 0, 0);
            }
            catch (System.Exception)
            {
                m_msDefragLib.DrawClusterEvent -= new MSDefragLib.MSDefragLib.DrawClusterHandler(DrawCluster);
            }
        }
    }
}
