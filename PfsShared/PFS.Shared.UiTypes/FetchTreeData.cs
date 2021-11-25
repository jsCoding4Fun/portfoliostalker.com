using System;

namespace PFS.Shared.UiTypes
{
    public enum ViewTreeEntry : int
    {
        Unknown = 0,
        Account,
        Portfolio,
        StockGroup,
    }

    /// <summary>
    /// 
    /// </summary>
    public class FetchTreeData
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string ParentName { get; set; }
        public ViewTreeEntry Type { get; set; }
    }
}
