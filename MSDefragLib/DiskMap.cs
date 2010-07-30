using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class DiskMap
    {
        public Int32 totalClusters;

        public DiskMap()
        {
            totalClusters = 0;

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
                }

                ResetClusterStates();
            }
        }

        public void AddCluster(Int32 idxCluster, eClusterState state)
        {
            Double clustersPerFilter = (Double)totalClusters / (Double)DiskFilteredDetails.TotalClusters;

            Int32 idxFilteredCluster = (Int32)(idxCluster / clustersPerFilter);

            DiskFilteredDetails.AddClusterState(idxFilteredCluster, state);
        }

        public void AddCluster(ItemStruct item, eClusterState state)
        {
            Double clustersPerFilter = (Double)totalClusters / (Double)DiskFilteredDetails.TotalClusters;

            foreach (Fragment fragment in item.FragmentList)
            {
                if (fragment.IsVirtual)
                    continue;

                AddCluster((Int32)fragment.Lcn, state);

                Int32 idxCluster = (Int32)(fragment.Lcn / clustersPerFilter);

                DiskFilteredDetails.AddClusterState(idxCluster, state);
            }

            //lock (DiskDetails)
            //{
            //    DiskDetails.AddCluster(idxCluster, state);
            //}
        }

        public IList<MapClusterState> GetFilteredClusters(Int32 clusterBegin, Int32 clusterEnd)
        {
            IList<MapClusterState> clusters = null;

            lock (DiskFilteredDetails)
            {
                Int32 filterBegin = 0;
                Int32 filterEnd = 1;

                Double clustersPerFilter = (Double)totalClusters / (Double)DiskFilteredDetails.TotalClusters;

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

        private void ReparseClusters4()
        {
            //ReparseClusters(0, totalClusters, eClusterState.Allocated);
            //lock (DiskDetails)
            //{
            //    lock (DiskFilteredDetails)
            //    {
            //        Int32 filterBegin = 0;
            //        Int32 filterEnd = DiskFilteredDetails.TotalClusters - 1;

            //        Double clustersPerFilter = (Double)DiskDetails.TotalClusters / (Double)DiskFilteredDetails.TotalClusters;

            //        for (Int32 filterIdx = filterBegin; filterIdx < filterEnd; filterIdx++)
            //        {
            //            Int32 clusterBegin = (Int32)(filterIdx * clustersPerFilter);
            //            Int32 clusterEnd = (Int32)(clusterBegin + clustersPerFilter);

            //            DiskFilteredDetails.ResetClusterStates(filterIdx);

            //            for (Int32 cluster = clusterBegin; cluster < clusterEnd && cluster < DiskDetails.TotalClusters; cluster++)
            //            {
            //                eClusterState state = eClusterState.Free;

            //                state = DiskDetails.GetClusterState(cluster);

            //                DiskFilteredDetails.AddClusterState(filterIdx,state);
            //            }

            //            DiskFilteredDetails.SetClusterDirty(filterIdx, true);
            //        }
            //    }
            //}
        }

        private void ReparseClusters2(Int32 clusterBegin, Int32 clusterEnd, eClusterState state)
        {
            //Double clustersPerFilter = (Double)totalClusters / (Double)DiskFilteredDetails.TotalClusters;

            //Int32 filterBegin = (Int32)(clusterBegin / clustersPerFilter);
            //Int32 filterEnd = (Int32)(clusterEnd / clustersPerFilter);

            //for (Int32 filterIdx = filterBegin; filterIdx < filterEnd; filterIdx++)
            //{
            //    if ((filterIdx < 0) || (filterIdx >= DiskFilteredDetails.TotalClusters))
            //        return;

            //    DiskFilteredDetails.AddClusterState(filterIdx, state);
            //    DiskFilteredDetails.SetClusterDirty(filterIdx, true);
            //}
        }

        public void SetClusterState(Int32 clusterBegin, Int32 clusterEnd, eClusterState state, Boolean updateFilteredClusters)
        {
            Double clustersPerFilter = (Double)totalClusters / (Double)DiskFilteredDetails.TotalClusters;

            Int32 filterBegin = (Int32)(clusterBegin / clustersPerFilter);
            Int32 filterEnd = (Int32)(clusterEnd / clustersPerFilter);

            for (Int32 filterIdx = filterBegin; filterIdx <= filterEnd; filterIdx++)
            {
                if ((filterIdx < 0) || (filterIdx >= DiskFilteredDetails.TotalClusters))
                    return;

                DiskFilteredDetails.AddClusterState(filterIdx, state);
                DiskFilteredDetails.SetClusterDirty(filterIdx, true);
            }
        }

        public void ResetClusterStates()
        {
            SetClusterState(0, totalClusters, eClusterState.Allocated, true);
        }

        //DiskMapDetails DiskDetails;
        DiskMapFilteredData DiskFilteredDetails;
    }
}
