using System;

namespace PFS.Shared.Types
{
    public enum UserEventType : int
    {
        Unknown = 0,
        OrderExpired,
        OrderBuy,
        OrderSell,
        AlarmUnder,
        AlarmOver,
    }

    public enum UserEventMode : int
    {
        Unknown = 0,
        Unread,
        UnreadImp,
        Read,
        Starred,
    }
}
