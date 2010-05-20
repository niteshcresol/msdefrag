using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    class DiskMap
    {
        public DiskMap(Int32 numClusters)
        {
            // Initialize clusters

            totalClusters = numClusters;
            clusterData = new List<ClusterState>(numClusters);

            for (UInt64 ii = 0; ii < (UInt64)totalClusters; ii++)
            {
                AddCluster(ii, eClusterState.Free);
            }

            // Initialize filtered clusters

            numFilteredClusters = 5733;
            filteredClusterData = new List<MapClusterState>(numFilteredClusters);

            for (Int32 ii = 0; ii <= numFilteredClusters; ii++)
            {
                MapClusterState cluster = new MapClusterState((UInt64)ii);

                filteredClusterData.Add(cluster);
            }

            clustersPerFilter = (Double)totalClusters / (Double)numFilteredClusters;

            // Initialize filters with current values

            ReparseClusters();
        }

        public void AddCluster(UInt64 idxCluster, eClusterState state)
        {
            ClusterState cluster = new ClusterState(idxCluster, state);
            clusterData.Add(cluster);
        }

        public IList<MapClusterState> GetFilteredClusters(UInt64 clusterBegin, UInt64 clusterEnd)
        {
            Int32 filterBegin = 0;
            Int32 filterEnd = 1;

            filterBegin = (Int32)(clusterBegin / clustersPerFilter);
            filterEnd = (Int32)(clusterEnd / clustersPerFilter);

            IList<MapClusterState> clusters = new List<MapClusterState>();
            //filteredClusterData.GetRange((Int32)filterBegin, (Int32)(filterEnd - filterBegin));

            foreach (MapClusterState cluster in filteredClusterData)
            {
                if (cluster.Dirty)
                {
                    clusters.Add(cluster);

                    cluster.Dirty = false;
                }
            }

            return clusters;
        }

        private void ReparseClusters()
        {
            Int32 filterBegin = 0;
            Int32 filterEnd = numFilteredClusters;

            for (Int32 filterIdx = filterBegin; filterIdx <= filterEnd; filterIdx++)
            {
                Int32 clusterBegin = (Int32)(filterIdx * clustersPerFilter);
                Int32 clusterEnd = (Int32)(clusterBegin + clustersPerFilter);

                filteredClusterData[filterIdx].ResetClusterStates();

                for (Int32 cluster = clusterBegin; cluster < clusterEnd && cluster < totalClusters; cluster++)
                {
                    filteredClusterData[filterIdx].AddClusterState(clusterData[cluster].State);
                }

                filteredClusterData[filterIdx].Dirty = true;
            }
        }

        public void SetClusterState(UInt64 idxCluster, eClusterState newState)
        {

            Int32 filterIdx = (Int32)(idxCluster / (UInt64)clustersPerFilter);

            if (filterIdx > numFilteredClusters) filterIdx = numFilteredClusters;

            eClusterState state = clusterData[(Int32)idxCluster].State;

            if (state != newState)
            {
                eClusterState maxState = filteredClusterData[filterIdx].MaxState;

                filteredClusterData[filterIdx].RemoveClusterState(state);
                filteredClusterData[filterIdx].AddClusterState(newState);

                eClusterState newMaxState = filteredClusterData[filterIdx].MaxState;

                if (maxState != newMaxState)
                {
                    filteredClusterData[filterIdx].Dirty = true;
                }

                clusterData[(Int32)idxCluster].State = newState;
            }
        }

        private Int32 totalClusters;
        public Int32 numFilteredClusters;
        Double clustersPerFilter;

        List<ClusterState> clusterData;
        List<MapClusterState> filteredClusterData;
    }
}
