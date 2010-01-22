using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSDefragLib;

namespace MSDefrag
{
    class DiskMapSquares
    {
        List<MapSquare> squares;

        public DiskMapSquares()
        {
            squares = new List<MapSquare>();
        }

        /// <summary>
        /// This function parses whole cluster list and updates square information.
        /// </summary>
        private void ParseClusters(int numSquares, List<eClusterState> clusterData)
        {
            squares = new List<MapSquare>(numSquares);

            Double clusterPerSquare = (Double)clusterData.Count / (Double)(numSquares);

            for (Int32 squareIndex = 0; squareIndex < numSquares; squareIndex++)
            {
                UInt64 clusterIndex = (UInt64)(squareIndex * clusterPerSquare);
                UInt64 lastClusterIndex = clusterIndex + (UInt64)clusterPerSquare - 1;

                if (lastClusterIndex > (UInt64)clusterData.Count - 1)
                {
                    lastClusterIndex = (UInt64)clusterData.Count - 1;
                }

                MapSquare square = new MapSquare(squareIndex, clusterIndex, lastClusterIndex);

                for (UInt64 jj = clusterIndex; jj <= lastClusterIndex; jj++)
                {
                    Int32 clusterColor = (Int32)clusterData[(Int32)jj];

                    square.numClusterStates[clusterColor]++;
                }

                square.SetMaxColor();

                squares.Add(square);
            }
        }
    }
}
