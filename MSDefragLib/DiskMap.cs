using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    class DiskMap
    {
        public DiskMap(UInt32 numClusters)
        {
            // Initialize clusters

            totalClusters = numClusters;

            clusterData2 = new Dictionary<UInt32, ClusterState>();
            //clusterData = new List<ClusterState>(numClusters);

            //for (UInt64 ii = 0; ii < (UInt64)totalClusters; ii++)
            //{
            //    AddCluster(ii, eClusterState.Free);
            //}

            // Initialize filtered clusters

            numFilteredClusters = 5733;
            filteredClusterData2 = new Dictionary<UInt32, MapClusterState>();
            //filteredClusterData = new List<MapClusterState>(numFilteredClusters);

            for (UInt32 ii = 0; ii <= numFilteredClusters; ii++)
            {
                MapClusterState cluster = new MapClusterState((UInt64)ii);

                filteredClusterData2.Add(ii, cluster);
                //filteredClusterData.Add(cluster);
            }

            clustersPerFilter = (Double)totalClusters / (Double)numFilteredClusters;

            // Initialize filters with current values

            ReparseClusters();
        }

        public void AddCluster(UInt32 idxCluster, eClusterState state)
        {
            ClusterState cluster = new ClusterState(idxCluster, state);
            //clusterData.Add(cluster);
            clusterData2[idxCluster] = cluster;
        }

        public IList<MapClusterState> GetFilteredClusters(UInt32 clusterBegin, UInt32 clusterEnd)
        {
            UInt64 filterBegin = 0;
            UInt64 filterEnd = 1;

            filterBegin = (UInt64)(clusterBegin / clustersPerFilter);
            filterEnd = (UInt64)(clusterEnd / clustersPerFilter);

            //IList<MapClusterState> clusters = new List<MapClusterState>();
            ////filteredClusterData.GetRange((Int32)filterBegin, (Int32)(filterEnd - filterBegin));

            //foreach (MapClusterState cluster in filteredClusterData)
            //{
            //    if (cluster.Dirty)
            //    {
            //        clusters.Add(cluster);

            //        cluster.Dirty = false;
            //    }
            //}

            List<MapClusterState> clusters =
                (from a in filteredClusterData2
                //(from a in filteredClusterData
                 where a.Value.Dirty == true && a.Value.Index >= filterBegin && a.Value.Index < filterEnd
                 select a.Value).ToList();

            return clusters;
        }

        private void ReparseClusters()
        {
            UInt32 filterBegin = 0;
            UInt32 filterEnd = numFilteredClusters;

            for (UInt32 filterIdx = filterBegin; filterIdx <= filterEnd; filterIdx++)
            {
                UInt32 clusterBegin = (UInt32)(filterIdx * clustersPerFilter);
                UInt32 clusterEnd = (UInt32)(clusterBegin + clustersPerFilter);

                filteredClusterData2[filterIdx].ResetClusterStates();
                //filteredClusterData[filterIdx].ResetClusterStates();

                for (UInt32 cluster = clusterBegin; cluster < clusterEnd && cluster < totalClusters; cluster++)
                {
                    eClusterState state = eClusterState.Free;

                    if (clusterData2.ContainsKey(cluster))
                    {
                        state = clusterData2[cluster].State;
                    }
                    filteredClusterData2[filterIdx].AddClusterState(state);
                    //filteredClusterData[filterIdx].AddClusterState(state);
                    //filteredClusterData[filterIdx].AddClusterState(clusterData[cluster].State);
                }

                filteredClusterData2[filterIdx].Dirty = true;
                //filteredClusterData[filterIdx].Dirty = true;
            }
        }

        public void SetClusterState(UInt32 idxCluster, eClusterState newState)
        {

            UInt32 filterIdx = (UInt32)(idxCluster / (UInt32)clustersPerFilter);

            if (filterIdx > numFilteredClusters) filterIdx = numFilteredClusters;

            eClusterState state = eClusterState.Busy;

            if (clusterData2.ContainsKey(idxCluster))
            {
                state = clusterData2[idxCluster].State;
            }
            else
            {
                AddCluster(idxCluster, eClusterState.Free);
            }

            //eClusterState state = clusterData[(Int32)idxCluster].State;

            if (state != newState)
            {
                eClusterState maxState = filteredClusterData2[filterIdx].MaxState;
                //eClusterState maxState = filteredClusterData[filterIdx].MaxState;

                filteredClusterData2[filterIdx].RemoveClusterState(state);
                //filteredClusterData[filterIdx].RemoveClusterState(state);
                filteredClusterData2[filterIdx].AddClusterState(newState);
                //filteredClusterData[filterIdx].AddClusterState(newState);

                eClusterState newMaxState = filteredClusterData2[filterIdx].MaxState;
                //eClusterState newMaxState = filteredClusterData[filterIdx].MaxState;

                if (maxState != newMaxState)
                {
                    filteredClusterData2[filterIdx].Dirty = true;
                    //filteredClusterData[filterIdx].Dirty = true;
                }

                clusterData2[idxCluster].State = newState;
                //clusterData[(Int32)idxCluster].State = newState;
            }
        }

        private UInt32 totalClusters;
        //private Int32 totalClusters;
        public UInt32 numFilteredClusters;
        Double clustersPerFilter;

        //List<ClusterState> clusterData;
        Dictionary<UInt32, MapClusterState> filteredClusterData2;
        //List<MapClusterState> filteredClusterData;
        Dictionary<UInt32, ClusterState> clusterData2;
    }
}
