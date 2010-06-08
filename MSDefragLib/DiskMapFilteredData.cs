using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    class DiskMapFilteredData
    {
        public DiskMapFilteredData(Int32 numFilteredClusters)
        {
            TotalClusters = numFilteredClusters;
        }

        public IList<MapClusterState> GetDirtyClusters(Int32 clusterBegin, Int32 clusterEnd)
        {
            List<MapClusterState> clusters = 
                (from a in ClusterData
                 where a.Dirty == true && a.Index >= clusterBegin && a.Index <= clusterEnd
                 select a).ToList();

            return clusters;
        }

        public IList<MapClusterState> GetAllClusters()
        {
            return ClusterData;
        }

        public void ResetClusterStates(Int32 index)
        {
            ClusterData[index].ResetClusterStates();
        }

        public void AddClusterState(Int32 index, eClusterState state)
        {
            ClusterData[index].AddClusterState(state);
        }

        public void SetClusterDirty(Int32 index, Boolean dirty)
        {
            ClusterData[index].Dirty = dirty;
        }

        private Int32 totalClusters;
        public Int32 TotalClusters
        {
            get { return totalClusters; }
            set
            {
                totalClusters = value;

                ClusterData = new List<MapClusterState>(totalClusters);

                for (Int32 ii = 0; ii < totalClusters; ii++)
                {
                    MapClusterState cluster = new MapClusterState(ii);

                    ClusterData.Add(cluster);
                }
            }
        }

        List<MapClusterState> ClusterData;
    }
}
