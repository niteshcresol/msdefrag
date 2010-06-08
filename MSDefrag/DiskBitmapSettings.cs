using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using MSDefragLib;

namespace MSDefrag
{
    class DiskBitmapSettings
    {
        #region Variables

        #region Settings

        public static Int32 borderOffset = 1;
        public static Int32 borderWidth = 2;

        private static Color ColorUnmovable = Color.Yellow;
        private static Color ColorAllocated = Color.LightBlue;
        private static Color ColorBusy = Color.Blue;
        private static Color ColorFree = Color.LightGray;
        private static Color ColorFragmented = Color.Orange;
        private static Color ColorMft = Color.Pink;
        private static Color ColorSpaceHog = Color.DarkCyan;
        private static Color ColorUnfragmented = Color.Green;

        #endregion

        private Int32 numSquares;
        public Int32 NumSquares { get { return numSquares; } }

        private Int32 numX;
        public Int32 NumX { get { return numX; } set { numX = value; numSquares = numX * numY; } }

        private Int32 numY;
        public Int32 NumY { get { return numY; } set { numY = value; numSquares = numX * numY; } }

        public Int32 offsetX;
        public Int32 offsetY;

        public Int32 squareSize;

        private Color[] colors;
        private SolidBrush[] solidBrushes;
        private LinearGradientBrush[] linearHorizontalGradientBrushes;
        private LinearGradientBrush[] linearVerticalGradientBrushes;
        private LinearGradientBrush[] linearForwardDiagonalGradientBrushes;

        public Bitmap[] mapSquareBitmaps;

        public Bitmap bitmap;

        public int borderOffsetX;
        public int borderOffsetY;
        public int mapWidth;
        public int mapHeight;

        #endregion

        #region Initialize

        public void Initialize(Int32 width, Int32 height, Int32 square)
        {
            InitializeDiskMap(width, height, square);
            InitColors();
            InitBrushes();
            InitMapSquareBitmaps();
            InitMapSquares();
        }

        public void InitializeDiskMap(Int32 width, Int32 height, Int32 square)
        {
            squareSize = square > 1 ? square - 1 : 1;

            Int32 availableWidth = width - borderOffset * 2 - borderWidth * 2;
            Int32 availableHeight = height - borderOffset * 2 - borderWidth * 2;

            NumX = availableWidth / squareSize;
            NumY = availableHeight / squareSize;

            mapWidth = NumX * squareSize + borderWidth * 2;
            mapHeight = NumY * squareSize + borderWidth * 2;

            borderOffsetX = (width - mapWidth) / 2;
            borderOffsetY = (height - mapHeight) / 2;

            offsetX = borderOffsetX + borderWidth;
            offsetY = borderOffsetY + borderWidth;

            bitmap = new Bitmap(width, height);
        }
        public void InitColors()
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

        public void InitBrushes()
        {
            InitSolidBrushes();
            InitHorizontalGradientBrushes();
            InitVerticalGradientBrushes();
            InitForwardDiagonalBrushes();
        }
        public void InitSolidBrushes()
        {
            solidBrushes = null;

            solidBrushes = new SolidBrush[(Int32)MSDefragLib.eClusterState.MaxValue];

            int ii = 0;

            foreach (Color col in colors)
            {
                solidBrushes[ii] = new SolidBrush(col);
            }
        }
        public void InitHorizontalGradientBrushes()
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
        public void InitVerticalGradientBrushes()
        {
            linearVerticalGradientBrushes = null;

            linearVerticalGradientBrushes = new LinearGradientBrush[(Int32)MSDefragLib.eClusterState.MaxValue];

            int ii = 0;

            foreach (Color col in colors)
            {
                Rectangle rec = new Rectangle(0, 0, squareSize, squareSize);

                linearVerticalGradientBrushes[ii] = GetLinearGradientBrushFromColor(col, true, rec, 0, 30, LinearGradientMode.Vertical);

                ii++;
            }
        }
        public void InitForwardDiagonalBrushes()
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

        public void InitMapSquareBitmaps()
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

        public void InitMapSquares()
        {
            mapSquares = new List<MapSquare>(NumSquares);

            for (Int16 ii = 0; ii < NumSquares; ii++)
            {
                mapSquares.Add(new MapSquare(ii));
            }
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
            foreach (MapClusterState cluster in clusters)
            {
                if (cluster.Index >= mapSquares.Count)
                    continue;

                MapSquare mapSquare = mapSquares[cluster.Index];

                if (mapSquare.maxClusterState != cluster.MaxState)
                {
                    mapSquare.maxClusterState = cluster.MaxState;
                    mapSquare.Dirty = true;
                }
            }
        }

        public void DrawMapSquares(Graphics graphics, Int32 indexBegin, Int32 indexEnd)
        {
            //List<MapSquare> dirtyMapSquares = 
            //    (from a in mapSquares
            //     where a.Dirty == true
            //     //where a.Dirty == true && a.SquareIndex >= indexBegin && a.SquareIndex < indexEnd && a.SquareIndex <= NumSquares
            //     select a).ToList();

            foreach (MapSquare mapSquare in mapSquares)
            {
                Int32 squareIndex = mapSquare.SquareIndex;
                Point squarePosition = new Point((Int32)(squareIndex % NumX), (Int32)(squareIndex / NumX));
                Int32 squareMapBitmapIndex = (Int32)mapSquares[squareIndex].maxClusterState;

                Rectangle rc = new Rectangle(offsetX + squarePosition.X * squareSize, offsetY + squarePosition.Y * squareSize, squareSize, squareSize);

                graphics.DrawImageUnscaled(mapSquareBitmaps[squareMapBitmapIndex], rc.Left, rc.Top);

                mapSquare.Dirty = false;
            }
        }

        public Rectangle DrawingArea
        {
            set { }
            get { return new Rectangle(borderOffsetX, borderOffsetY, mapWidth, mapHeight); }
        }

        public Rectangle OutsideBorder
        {
            set { }
            get { return new Rectangle(borderOffsetX, borderOffsetY, mapWidth - 1, mapHeight - 1); }
        }

        public Rectangle InsideBorder
        {
            set { }
            get { return new Rectangle(borderOffsetX + borderWidth - 1, borderOffsetY + borderWidth - 1, mapWidth - borderWidth * 2 + 1, mapHeight - borderWidth * 2 + 1); }
        }

        public List<MapSquare> mapSquares;
    }
}
