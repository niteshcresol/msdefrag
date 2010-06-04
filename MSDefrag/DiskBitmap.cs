using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using MSDefragLib;
using System.Windows.Forms;

namespace MSDefrag
{
    class DiskBitmap : PictureBox
    {
        #region Constructor

        public DiskBitmap()
        {
            BitmapSettings = new DiskBitmapSettings();
        }

        #endregion

        #region Initialization

        public void Initialize(GuiSettings guiSettings)
        {
            lock (BitmapSettings)
            {
                squareSize = guiSettings.SquareSize;

                InitializeDiskMap(squareSize);

                BitmapSettings.InitColors();
                BitmapSettings.InitBrushes();
                BitmapSettings.InitMapSquareBitmaps();
                BitmapSettings.InitMapSquares();

                DrawMapSquares(0, BitmapSettings.NumSquares);
            }
        }

        public void InitializeDiskMap(Int32 square)
        {
            BitmapSettings.InitializeDiskMap(Width, Height, square);

            Image = BitmapSettings.bitmap;

            graphics = Graphics.FromImage(BitmapSettings.bitmap);

            Rectangle rec = new Rectangle(BitmapSettings.borderOffsetX, BitmapSettings.borderOffsetY, BitmapSettings.mapWidth, BitmapSettings.mapHeight);

            LinearGradientBrush brush = new LinearGradientBrush(rec, Color.Black, Color.White, LinearGradientMode.ForwardDiagonal);

            graphics.FillRectangle(brush, rec);

            Pen hiPen = Pens.White;
            Pen loPen = Pens.DarkGray;

            // Outside border
            Rectangle outRec = new Rectangle(rec.Left, rec.Top, rec.Width - 1, rec.Height - 1);

            graphics.DrawLine(hiPen, outRec.Left, outRec.Top, outRec.Right, outRec.Top);
            graphics.DrawLine(loPen, outRec.Right, outRec.Top, outRec.Right, outRec.Bottom);
            graphics.DrawLine(loPen, outRec.Right, outRec.Bottom, outRec.Left, outRec.Bottom);
            graphics.DrawLine(hiPen, outRec.Left, outRec.Bottom, outRec.Left, outRec.Top);

            // Inside border
            Rectangle inRec = new Rectangle(
                rec.Left + DiskBitmapSettings.borderWidth - 1, rec.Top + DiskBitmapSettings.borderWidth - 1,
                rec.Width - DiskBitmapSettings.borderWidth * 2 + 1, rec.Height - DiskBitmapSettings.borderWidth * 2 + 1);

            graphics.DrawLine(loPen, inRec.Left, inRec.Top, inRec.Right, inRec.Top);
            graphics.DrawLine(hiPen, inRec.Right, inRec.Top, inRec.Right, inRec.Bottom);
            graphics.DrawLine(hiPen, inRec.Right, inRec.Bottom, inRec.Left, inRec.Bottom);
            graphics.DrawLine(loPen, inRec.Left, inRec.Bottom, inRec.Left, inRec.Top);

            Invalidate();
        }

        #endregion

        /// <summary>
        /// This function parses whole cluster list and updates square information.
        /// </summary>
        public void AddFilteredClusters(IList<MapClusterState> clusters)
        {
            lock (BitmapSettings)
            {
                BitmapSettings.AddFilteredClusters(clusters);
                DrawMapSquares(0, BitmapSettings.NumSquares);
            }
        }

        #region Drawing

        private void DrawMapSquares(Int32 indexBegin, Int32 indexEnd)
        {
            List<MapSquare> dirtyMapSquares = BitmapSettings.GetDirtySquares(indexBegin, indexEnd);

            foreach (MapSquare mapSquare in dirtyMapSquares)
            {
                Int32 squareIndex = mapSquare.SquareIndex;
                Point squarePosition = new Point((Int32)(squareIndex % BitmapSettings.NumX), (Int32)(squareIndex / BitmapSettings.NumX));
                Int32 squareMapBitmapIndex = (Int32)BitmapSettings.mapSquares[squareIndex].maxClusterState;

                Rectangle rc = new Rectangle(BitmapSettings.offsetX + squarePosition.X * BitmapSettings.squareSize, BitmapSettings.offsetY + squarePosition.Y * BitmapSettings.squareSize, BitmapSettings.squareSize, BitmapSettings.squareSize);

                graphics.DrawImageUnscaled(BitmapSettings.mapSquareBitmaps[squareMapBitmapIndex], rc.Left, rc.Top);

                mapSquare.Dirty = false;
                //DiskPicture.Invalidate(rc);
            }

            Invalidate();
        }

        public void DrawAllMapSquares()
        {
            lock (BitmapSettings)
            {
                DrawMapSquares(0, BitmapSettings.NumSquares);
            }
        }

        public void SetBusy(Boolean busy)
        {
            SystemBusy = busy;
        }


        public Int32 NumSquares
        {
            get { return BitmapSettings.NumSquares; }
        }

        #endregion

        #region Variables

        #region Gui

        private DiskBitmapSettings BitmapSettings;

        private Int32 squareSize;

        private Graphics graphics;

        private Boolean SystemBusy;

        #endregion

        #endregion
    }
}
