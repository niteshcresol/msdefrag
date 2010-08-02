using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class DiskMap
    {
        private Int32 totalClusters;
        public Int32 TotalClusters
        {
            get { return totalClusters; }
            set
            {
                totalClusters = value;
                clustersPerFilter = (Double)TotalClusters / (Double)DiskFilteredDetails.TotalClusters;
            }
        }

        private Double clustersPerFilter;

        public DiskMap()
        {
            totalClusters = 0;
            clustersPerFilter = 1;

            DiskFilteredDetails = new DiskMapFilteredData(0);
        }

        public Int32 NumFilteredClusters
        {
            get { return DiskFilteredDetails.TotalClusters; }

            set
            {
                lock (DiskFilteredDetails)
                {
                    DiskFilteredDetails.TotalClusters = value;

                    clustersPerFilter = (Double)totalClusters / (Double)DiskFilteredDetails.TotalClusters;
                }

                ResetClusterStates();
            }
        }

        public void AddCluster(Int32 idxCluster, eClusterState state)
        {
            Int32 idxFilteredCluster = (Int32)(idxCluster / clustersPerFilter);

            DiskFilteredDetails.AddClusterState(idxFilteredCluster, state);
        }

        public void AddCluster(ItemStruct item, eClusterState state)
        {
            foreach (Fragment fragment in item.FragmentList)
            {
                if (fragment.IsVirtual)
                    continue;

                AddCluster((Int32)fragment.Lcn, state);

                Int32 idxCluster = (Int32)(fragment.Lcn / clustersPerFilter);

                DiskFilteredDetails.AddClusterState(idxCluster, state);
            }
        }

        public IList<MapClusterState> GetFilteredClusters(Int32 clusterBegin, Int32 clusterEnd)
        {
            IList<MapClusterState> clusters = null;

            lock (DiskFilteredDetails)
            {
                Int32 filterBegin = 0;
                Int32 filterEnd = 1;

                filterBegin = (Int32)(clusterBegin / clustersPerFilter);
                filterEnd = (Int32)(clusterEnd / clustersPerFilter);

                clusters = DiskFilteredDetails.GetDirtyClusters(filterBegin, filterEnd);
                //clusters = DiskFilteredDetails.GetAllClusters();
            }

            return clusters;
        }

        public IList<MapClusterState> GetAllFilteredClusters()
        {
            IList<MapClusterState> clusters = null;

            lock (DiskFilteredDetails)
            {
                clusters = DiskFilteredDetails.GetAllClusters();
            }

            return clusters;
        }

        public void SetClusterState(Int32 clusterBegin, Int32 clusterEnd, eClusterState state, Boolean updateFilteredClusters)
        {
            Int32 filterBegin = (Int32)(clusterBegin / clustersPerFilter);
            Int32 filterEnd = (Int32)(clusterEnd / clustersPerFilter);

            for (Int32 filterIdx = filterBegin; filterIdx <= filterEnd; filterIdx++)
            {
                if ((filterIdx < 0) || (filterIdx >= DiskFilteredDetails.TotalClusters))
                    return;

                DiskFilteredDetails.AddClusterState(filterIdx, state);
                //DiskFilteredDetails.SetClusterDirty(filterIdx, true);
            }
        }

        public void ResetClusterStates()
        {
            Int32 filterBegin = 0;
            Int32 filterEnd = NumFilteredClusters;

            for (Int32 filterIdx = filterBegin; filterIdx < filterEnd; filterIdx++)
            {
                DiskFilteredDetails.ResetClusterStates(filterIdx);
                //DiskFilteredDetails.SetClusterDirty(filterIdx, true);
            }
        }

        DiskMapFilteredData DiskFilteredDetails;
    }
}
