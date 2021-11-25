namespace PFS.Shared.Types
{
    public enum StockAlarmType : int
    {
        Unknown = 0,
        Under,
        Over,
        UnderWatch,
        UnderWatchP,
        OverWatch,
        OverWatchP,
    }

    /* 
     * Under/Over -alarms:
     * -------------------
     *  - Can have as many as wanted of both
     *  - Both has special column available for report, example 'Under %', shows how many procent until alarm is hit
     *    so that negative -n% means that alarm is not yet hit, and +n% means alarm is active
     * 
     * UnderWatch/UnderWatchP/OverWatch/OverWatchP:
     * --------------------------------------------
     *  - Under/Over part works like above, but has additional watch value either as level or as prosentage from main
     *    value. This creates pre-alarm zone, called Watch -area, that is kind of warning area that alarm is almost 
     *    reached to example set buy order for that alarm level. 
     *  - Normal Stock Table -report doesnt show this at all, but there is special dynamic Watch -report where can 
     *    see all stocks listed those valuation was on that day on Watch area (or related Alarm area)
     */
}
