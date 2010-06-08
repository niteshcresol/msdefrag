using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    class DiskMapDetails
    {
        public DiskMapDetails(Int32 numClusters)
        {
            TotalClusters = numClusters;

            ClusterData = new List<ClusterState>(numClusters);

            for (Int32 ii = 0; ii <= TotalClusters; ii++)
            {
                AddCluster(ii, eClusterState.Free);
            }
        }

        public void AddCluster(Int32 idxCluster, eClusterState state)
        {
            ClusterState cluster = new ClusterState(idxCluster, state);

            ClusterData.Add(cluster);
        }

        public void SetClusterState(Int32 clusterBegin, Int32 clusterEnd, eClusterState state)
        {
            if ((clusterBegin < 0) || (clusterBegin > TotalClusters) ||
                (clusterEnd < 0) || (clusterEnd > TotalClusters))
            {
                return;
            }

            for (Int32 idxCluster = clusterBegin; idxCluster < clusterEnd; idxCluster++)
            {
                ClusterData[idxCluster].State = state;
            }
        }

        public eClusterState GetClusterState(Int32 index)
        {
            return ClusterData[index].State;
        }

        public Int32 TotalClusters { get; set; }

        List<ClusterState> ClusterData;
    }
}
