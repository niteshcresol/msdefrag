using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class DiskMap
    {
        public Int32 a;

        public DiskMap(Int32 numClusters)
        {
            a = numClusters;
            //DiskDetails = new DiskMapDetails(numClusters);
            DiskFilteredDetails = new DiskMapFilteredData(5733);
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

                ReparseClusters();
            }
        }

        public void AddCluster(Int32 idxCluster, eClusterState state)
        {
            //lock (DiskDetails)
            //{
            //    DiskDetails.AddCluster(idxCluster, state);
            //}
        }

        public void AddCluster(ItemStruct item, eClusterState state)
        {
            Double clustersPerFilter = (Double)a / (Double)DiskFilteredDetails.TotalClusters;

            foreach (Fragment fragment in item.FragmentList)
            {
                if (fragment.IsVirtual)
                    continue;

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

                Double clustersPerFilter = (Double)a / (Double)DiskFilteredDetails.TotalClusters;

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

        private void ReparseClusters()
        {
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

        private void ReparseClusters(Int32 clusterBegin, Int32 clusterEnd, eClusterState state)
        {
            //lock (DiskFilteredDetails)
            //{
                Double clustersPerFilter = (Double)a / (Double)DiskFilteredDetails.TotalClusters;

                Int32 filterBegin = (Int32)(clusterBegin / clustersPerFilter);
                Int32 filterEnd = (Int32)(clusterEnd / clustersPerFilter);

                for (Int32 filterIdx = filterBegin; filterIdx < filterEnd; filterIdx++)
                {
                    if ((filterIdx < 0) || (filterIdx >= DiskFilteredDetails.TotalClusters))
                        return;

                    DiskFilteredDetails.AddClusterState(filterIdx, state);
                    DiskFilteredDetails.SetClusterDirty(filterIdx, true);
                }
            //}
        }

        public void SetClusterState(Int32 clusterBegin, Int32 clusterEnd, eClusterState state, Boolean updateFilteredClusters)
        {
            //lock (DiskDetails)
            //{
            //    DiskDetails.SetClusterState(clusterBegin, clusterEnd, state);

            //    if (updateFilteredClusters)
            //    {
                    ReparseClusters(clusterBegin, clusterEnd, state);
            //    }
            //}
        }

        //DiskMapDetails DiskDetails;
        DiskMapFilteredData DiskFilteredDetails;
    }
}
