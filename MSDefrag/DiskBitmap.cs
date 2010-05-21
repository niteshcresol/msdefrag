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
        #region Settings

        private static Int32 borderOffset = 1;
        private static Int32 borderWidth = 2;

        private static Color ColorUnmovable = Color.Yellow;
        private static Color ColorAllocated = Color.LightBlue;
        private static Color ColorBusy = Color.Blue;
        private static Color ColorFree = Color.Gray;
        private static Color ColorFragmented = Color.Orange;
        private static Color ColorMft = Color.Pink;
        private static Color ColorSpaceHog = Color.DarkCyan;
        private static Color ColorUnfragmented = Color.Green;

        #endregion

        #region Constructor

        public DiskBitmap()
        {
        }

        public void Dispose44()
        {
            foreach (SolidBrush br in solidBrushes)
            {
                br.Dispose();
            }

            foreach (LinearGradientBrush br in linearHorizontalGradientBrushes)
            {
                br.Dispose();
            }

            foreach (LinearGradientBrush br in linearVerticalGradientBrushes)
            {
                br.Dispose();
            }

            foreach (LinearGradientBrush br in linearForwardDiagonalGradientBrushes)
            {
                br.Dispose();
            }

            foreach (Bitmap b in mapSquareBitmaps)
            {
                b.Dispose();
            }

            bitmap.Dispose();
            graphics.Dispose();

            GC.SuppressFinalize(this);
        }

        #endregion

        #region Initialization

        public void Initialize(Int32 square)
        {
            squareSize = square;

            InitializeDiskMap(square);

            InitColors();
            InitBrushes();
            InitMapSquareBitmaps();

            InitMapSquares();
        }

        public void InitializeDiskMap(Int32 square)
        {
            Int32 height = Height;
            Int32 width = Width;

            squareSize = square > 1 ? square - 1 : 1;

            Int32 availableWidth = width - borderOffset * 2 - borderWidth * 2;
            Int32 availableHeight = height - borderOffset * 2 - borderWidth * 2;

            NumX = availableWidth / squareSize;
            NumY = availableHeight / squareSize;

            int mapWidth = NumX * squareSize + borderWidth * 2;
            int mapHeight = NumY * squareSize + borderWidth * 2;

            int borderOffsetX = (width - mapWidth) / 2;
            int borderOffsetY = (height - mapHeight) / 2;

            offsetX = borderOffsetX + borderWidth;
            offsetY = borderOffsetY + borderWidth;

            bitmap = new Bitmap(width, height);

            Image = bitmap;

            graphics = Graphics.FromImage(bitmap);

            Rectangle rec = new Rectangle(borderOffsetX, borderOffsetY, mapWidth, mapHeight);

            LinearGradientBrush brush = new LinearGradientBrush(rec, Color.Black, Color.Black, LinearGradientMode.ForwardDiagonal);

            graphics.FillRectangle(brush, rec);

            // Outside border
            graphics.DrawLine(Pens.White,
                borderOffsetX, borderOffsetY, 
                borderOffsetX + mapWidth, borderOffsetY);

            graphics.DrawLine(Pens.DarkGray,
                borderOffsetX + mapWidth - 1, borderOffsetY,
                borderOffsetX + mapWidth - 1, borderOffsetY + mapHeight - 1);

            graphics.DrawLine(Pens.DarkGray,
                borderOffsetX + mapWidth - 1, borderOffsetY + mapHeight - 1,
                borderOffsetX, borderOffsetY + mapHeight - 1);

            graphics.DrawLine(Pens.White,
                borderOffsetX, borderOffsetY + mapHeight,
                borderOffsetX, borderOffsetY);

            // Inside border
            graphics.DrawLine(Pens.DarkGray, 
                borderOffsetX + borderWidth - 1, borderOffsetY + borderWidth - 1,
                borderOffsetX + mapWidth - borderWidth, borderOffsetY + borderWidth - 1);

            graphics.DrawLine(Pens.White,
                borderOffsetX + mapWidth - borderWidth, borderOffsetY + borderWidth - 1,
                borderOffsetX + mapWidth - borderWidth, borderOffsetY + mapHeight - borderWidth);

            graphics.DrawLine(Pens.White,
                borderOffsetX + mapWidth - borderWidth, borderOffsetY + mapHeight - borderWidth,
                borderOffsetX + borderWidth - 1, borderOffsetY + mapHeight - borderWidth);

            graphics.DrawLine(Pens.DarkGray,
                borderOffsetX + borderWidth - 1, borderOffsetY + mapHeight - borderWidth,
                borderOffsetX + borderWidth - 1, borderOffsetY + borderWidth - 1);

            Invalidate();
        }

        private void InitColors()
        {
            colors = null;

            colors = new Color[(Int32)MSDefragLib.eClusterState.MaxValue];

            colors[(Int32)MSDefragLib.eClusterState.Unmovable] = ColorUnmovable;
            colors[(Int32)MSDefragLib.eClusterState.Allocated] = ColorAllocated;
            colors[(Int32)MSDefragLib.eClusterState.Busy] = ColorBusy;
            colors[(Int32)MSDefragLib.eClusterState.Free] = ColorFree;
            colors[(Int32)MSDefragLib.eClusterState.Fragmented] = ColorFragmented;
            colors[(Int32)MSDefragLib.eClusterState.Mft] = ColorMft;
            colors[(Int32)MSDefragLib.eClusterState.SpaceHog] = ColorSpaceHog;
            colors[(Int32)MSDefragLib.eClusterState.Unfragmented] = ColorUnfragmented;
        }

        private void InitBrushes()
        {
            InitSolidBrushes();
            InitHorizontalGradientBrushes();
            InitVerticalGradientBrushes();
            InitForwardDiagonalBrushes();
        }
        private void InitSolidBrushes()
        {
            solidBrushes = null;

            solidBrushes = new SolidBrush[(Int32)MSDefragLib.eClusterState.MaxValue];

            int ii = 0;

            foreach (Color col in colors)
            {
                solidBrushes[ii] = new SolidBrush(col);
            }
        }
        private void InitHorizontalGradientBrushes()
        {
            linearHorizontalGradientBrushes = null;

            linearHorizontalGradientBrushes = new LinearGradientBrush[(Int32)MSDefragLib.eClusterState.MaxValue];

            int ii = 0;

            foreach (Color col in colors)
            {
                Rectangle rec = new Rectangle(0, 0, squareSize, squareSize);

                linearHorizontalGradientBrushes[ii] = GetLinearGradientBrushFromColor(col, false, rec, 20, 70, LinearGradientMode.Horizontal);

                ii++;
            }
        }
        private void InitVerticalGradientBrushes()
        {
            linearVerticalGradientBrushes = null;

            linearVerticalGradientBrushes = new LinearGradientBrush[(Int32)MSDefragLib.eClusterState.MaxValue];

            int ii = 0;

            foreach (Color col in colors)
            {
                Rectangle rec = new Rectangle(0, 0, squareSize, squareSize);

                linearVerticalGradientBrushes[ii] = GetLinearGradientBrushFromColor(col, true, rec, 0, 100, LinearGradientMode.Vertical);

                ii++;
            }
        }
        private void InitForwardDiagonalBrushes()
        {
            linearForwardDiagonalGradientBrushes = null;

            linearForwardDiagonalGradientBrushes = new LinearGradientBrush[(Int32)MSDefragLib.eClusterState.MaxValue];

            int ii = 0;

            foreach (Color col in colors)
            {
                Rectangle rec = new Rectangle(0, 0, squareSize, squareSize);

                linearForwardDiagonalGradientBrushes[ii] = GetLinearGradientBrushFromColor(col, false, rec, 80, 70, LinearGradientMode.ForwardDiagonal);

                ii++;
            }
        }

        private void InitMapSquareBitmaps()
        {
            mapSquareBitmaps = new Bitmap[(Int32)MSDefragLib.eClusterState.MaxValue];

            for (int ii = 0; ii < (Int32)MSDefragLib.eClusterState.MaxValue; ii++)
            {
                mapSquareBitmaps[(Int32)ii] = new Bitmap(squareSize, squareSize);

                using (Graphics g1 = Graphics.FromImage(mapSquareBitmaps[(Int32)ii]))
                {
                    Rectangle rec = new Rectangle(0, 0, squareSize, squareSize);

                    if (ii == (Int32)MSDefragLib.eClusterState.Free)
                    {
                        g1.FillRectangle(linearVerticalGradientBrushes[(Int32)ii], rec);
                    }
                    else
                    {
                        g1.FillRectangle(linearForwardDiagonalGradientBrushes[(Int32)ii], rec);
                    }
                }
            }
        }

        private void InitMapSquares()
        {
            mapSquares = null;

            mapSquares = new List<MapSquare>(NumSquares);

            for (Int16 ii = 0; ii < NumSquares + 1; ii++)
            {
                mapSquares.Add(new MapSquare(ii));
            }

            DrawMapSquares(0, NumSquares);
        }

        #endregion

        #region Helper functions

        private static LinearGradientBrush GetLinearGradientBrushFromColor(
            Color color, Boolean bright, Rectangle rec, Byte brightness, Byte darkness, LinearGradientMode mode)
        {
            LinearGradientBrush brush = null;

            Byte r = (Byte)(((color.R - darkness) > 0) ? (color.R - darkness) : 0);
            Byte g = (Byte)(((color.G - darkness) > 0) ? (color.G - darkness) : 0);
            Byte b = (Byte)(((color.B - darkness) > 0) ? (color.B - darkness) : 0);

            Byte r2 = (Byte)(((color.R + brightness) < Byte.MaxValue) ? (color.R + brightness) : Byte.MaxValue);
            Byte g2 = (Byte)(((color.G + brightness) < Byte.MaxValue) ? (color.G + brightness) : Byte.MaxValue);
            Byte b2 = (Byte)(((color.B + brightness) < Byte.MaxValue) ? (color.B + brightness) : Byte.MaxValue);

            Color darkColor = Color.FromArgb(r, g, b);
            Color brightColor = Color.FromArgb(r2, g2, b2);

            if (bright)
            {
                darkColor = Color.FromArgb(r2, g2, b2);
                brightColor = Color.FromArgb(r, g, b);
            }

            brush = new LinearGradientBrush(rec, brightColor, darkColor, mode);

            return brush;
        }

        #endregion

        /// <summary>
        /// This function parses whole cluster list and updates square information.
        /// </summary>
        public void AddFilteredClusters(IList<MapClusterState> clusters)
        {
            lock (mapSquares)
            {
                foreach (MapClusterState cluster in clusters)
                {
                    MapSquare mapSquare = mapSquares[(Int32)cluster.Index];

                    if (mapSquare.maxClusterState != cluster.MaxState)
                    {
                        mapSquare.maxClusterState = cluster.MaxState;
                        mapSquare.Dirty = true;
                    }
                }

                if (SystemBusy == false)
                {
                    DrawMapSquares(0, numSquares);
                }
            }
        }

        #region Drawing

        private void DrawMapSquares(Int32 indexBegin, Int32 indexEnd)
        {
            lock (mapSquares)
            {
                List<MapSquare> dirtyMapSquares =
                    (from a in mapSquares
                     where a.Dirty == true && a.SquareIndex >= indexBegin && a.SquareIndex < indexEnd
                     select a).ToList();

                foreach (MapSquare mapSquare in dirtyMapSquares)
                {
                    Int32 squareIndex = mapSquare.SquareIndex;
                    Point squarePosition = new Point((Int32)(squareIndex % NumX), (Int32)(squareIndex / NumX));
                    Int32 squareMapBitmapIndex = (Int32)mapSquares[squareIndex].maxClusterState;

                    Rectangle rc = new Rectangle(offsetX + squarePosition.X * squareSize, offsetY + squarePosition.Y * squareSize, squareSize, squareSize);

                    graphics.DrawImageUnscaled(mapSquareBitmaps[squareMapBitmapIndex], rc.Left, rc.Top);

                    mapSquare.Dirty = false;
                    //DiskPicture.Invalidate(rc);
                }

                //for (Int32 ii = indexBegin; ii < indexEnd; ii++)
                //{
                //    Int32 posX = (Int32)(ii % NumX);
                //    Int32 posY = (Int32)(ii / NumX);

                //    Int32 squareMapBitmapIndex = (Int32)mapSquares[ii].maxClusterState;

                //    if (mapSquares[ii].Dirty == true)
                //    {
                //        Rectangle rc = new Rectangle(offsetX + posX * squareSize, offsetY + posY * squareSize, squareSize, squareSize);

                //        graphics.DrawImageUnscaled(mapSquareBitmaps[squareMapBitmapIndex], rc.Left, rc.Top);

                //        mapSquares[ii].Dirty = false;
                //    }
                //}
            }

            Invalidate();
        }

        public void DrawAllMapSquares()
        {
            DrawMapSquares(0, NumSquares);
        }

        public void SetBusy(Boolean busy)
        {
            SystemBusy = busy;
        }

        #endregion

        #region Variables

        #region Gui

        private Int32 numSquares;
        public Int32 NumSquares { get { return numSquares; } }

        private Int32 numX;
        public Int32 NumX { get { return numX; } set { numX = value; numSquares = numX * numY; } }

        private Int32 numY;
        public Int32 NumY { get { return numY; } set { numY = value; numSquares = numX * numY; } }

        private Int32 offsetX;
        private Int32 offsetY;

        private Int32 squareSize;

        private Color[] colors;
        private SolidBrush[] solidBrushes;
        private LinearGradientBrush[] linearHorizontalGradientBrushes;
        private LinearGradientBrush[] linearVerticalGradientBrushes;
        private LinearGradientBrush[] linearForwardDiagonalGradientBrushes;

        private Bitmap[] mapSquareBitmaps;

        public Bitmap bitmap;
        private Graphics graphics;

        private Boolean SystemBusy;

        #endregion

        #region Other

        private List<MapSquare> mapSquares;

        #endregion

        #endregion
    }
}
