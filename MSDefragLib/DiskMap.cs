using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class DiskMap
    {
        public DiskMap(Int32 numClusters)
        {
            totalClusters = numClusters;

            clusterData = new List<ClusterState>(numClusters);

            for (Int32 ii = 0; ii <= totalClusters; ii++)
            {
                AddCluster(ii, eClusterState.Free);
            }

            NumFilteredClusters = 5733;
        }

        public Int32 NumFilteredClusters
        {
            get { return numFilteredClusters; }

            set
            {
                numFilteredClusters = value + 1;

                if (filteredClusterData == null)
                {
                    filteredClusterData = new List<MapClusterState>(numFilteredClusters);
                }
                else
                {
                    lock (filteredClusterData)
                    {
                        filteredClusterData = new List<MapClusterState>(numFilteredClusters);
                    }
                }

                lock (filteredClusterData)
                {
                    for (Int32 ii = 0; ii < numFilteredClusters; ii++)
                    {
                        MapClusterState cluster = new MapClusterState(ii);

                        filteredClusterData.Add(cluster);
                    }

                    clustersPerFilter = (Double)totalClusters / (Double)numFilteredClusters;

                    // Initialize filters with current values
                    ReparseClusters();
                }
            }
        }

        public void AddCluster(Int32 idxCluster, eClusterState state)
        {
            ClusterState cluster = new ClusterState(idxCluster, state);

            clusterData.Add(cluster);
        }

        public IList<MapClusterState> GetFilteredClusters(Int32 clusterBegin, Int32 clusterEnd)
        {
            List<MapClusterState> clusters;

            lock (filteredClusterData)
            {
                Int32 filterBegin = 0;
                Int32 filterEnd = 1;

                filterBegin = (Int32)(clusterBegin / clustersPerFilter);
                filterEnd = (Int32)(clusterEnd / clustersPerFilter);

                clusters =
                    (from a in filteredClusterData
                     where a.Dirty == true && a.Index >= filterBegin && a.Index <= filterEnd
                     select a).ToList();
            }

            return clusters;
        }

        public IList<MapClusterState> GetAllFilteredClusters()
        {
            List<MapClusterState> clusters;

            lock (filteredClusterData)
            {
                Int32 filterBegin = 0;
                Int32 filterEnd = numFilteredClusters;

                clusters =
                    (from a in filteredClusterData
                     where a.Index >= filterBegin && a.Index < filterEnd
                     select a).ToList();
            }

            return clusters;
        }

        private void ReparseClusters()
        {
            Int32 filterBegin = 0;
            Int32 filterEnd = numFilteredClusters;

            for (Int32 filterIdx = filterBegin; filterIdx < filterEnd; filterIdx++)
            {
                Int32 clusterBegin = (Int32)(filterIdx * clustersPerFilter);
                Int32 clusterEnd = (Int32)(clusterBegin + clustersPerFilter);

                filteredClusterData[(Int32)filterIdx].ResetClusterStates();

                for (Int32 cluster = clusterBegin; cluster < clusterEnd && cluster < totalClusters; cluster++)
                {
                    eClusterState state = eClusterState.Free;

                    state = clusterData[cluster].State;

                    filteredClusterData[filterIdx].AddClusterState(state);
                }

                filteredClusterData[filterIdx].Dirty = true;
            }
        }

        public void SetClusterState(Int32 clusterBegin, Int32 clusterEnd, eClusterState newState, Boolean updateFilteredClusters)
        {
            if ((clusterBegin < 0) || (clusterBegin > totalClusters) ||
                (clusterEnd < 0) || (clusterEnd > totalClusters))
            {
                return;
            }

            for (Int32 idxCluster = clusterBegin; idxCluster < clusterEnd; idxCluster++)
            {
                SetClusterState(idxCluster, newState, updateFilteredClusters);
            }
        }

        public void SetClusterState(Int32 idxCluster, eClusterState newState, Boolean updateFilteredClusters)
        {
            lock (filteredClusterData)
            {

                Int32 filterIdx = (Int32)(idxCluster / (Int32)clustersPerFilter);

                if (filterIdx >= numFilteredClusters)
                    filterIdx = numFilteredClusters - 1;

                eClusterState state = eClusterState.Busy;

                state = clusterData[idxCluster].State;

                if (state != newState)
                {
                    try
                    {
                        eClusterState maxState = filteredClusterData[filterIdx].MaxState;

                        filteredClusterData[filterIdx].RemoveClusterState(state);
                        filteredClusterData[filterIdx].AddClusterState(newState);

                        eClusterState newMaxState = filteredClusterData[filterIdx].MaxState;

                        if (maxState != newMaxState)
                        {
                            filteredClusterData[filterIdx].Dirty = true;
                        }
                    }
                    catch (System.Collections.Generic.KeyNotFoundException)
                    {
                    }

                    clusterData[idxCluster].State = newState;
                }
            }
        }

        private Int32 totalClusters;
        private Int32 numFilteredClusters;
        Double clustersPerFilter;

        List<ClusterState> clusterData;
        List<MapClusterState> filteredClusterData;
    }
}
