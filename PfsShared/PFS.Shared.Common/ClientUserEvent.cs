/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;

using PFS.Shared.Types;

namespace PFS.Shared.Common
{
    // Present's a single event on LocalEvent's list indicating Order Expirations & Alarms etc events worth of noticing
    public class ClientUserEvent
    {
        // 'Field' is 'Param' = 'Value'
        // ASCII 31 (0x1F) Unit Separator => used to separate 'Field's to create Content string
        const char _unitSeparator = ((char)31);

        string _content = string.Empty;

        public string Content { get { return _content; } }

        static protected int _runningID = 1;    // Next available ID for assigning (memory only ID)
        public int ID { get; internal set; }    // Runtime only, but required for Update/Delete operations 

        // Generic Fields
        static public implicit operator DateTime(ClientUserEvent ev) { return DateTime.ParseExact(ev.GetValue("D"), "yyMMdd", CultureInfo.InvariantCulture); }
        static public implicit operator UserEventType(ClientUserEvent ev) { return TypeShortcuts.Single(t => t.Item2 == ev.GetValue("T")).Item1; }
        static public implicit operator Guid(ClientUserEvent ev) { return ev.GetValue("S") != null ? Guid.Parse(ev.GetValue("S")) : Guid.Empty; }
        static public implicit operator UserEventMode(ClientUserEvent ev) { return ModeShortcuts.Single(t => t.Item2 == ev.GetValue("M")).Item1; }
        public string Portfolio() { return GetValue("P"); }

        public ClientUserEvent(string content)
        {
            _content = content;
            ID = _runningID++;
        }

        #region ORDER Expired / Buy level reach / Sell level reach

        static public ClientUserEvent CreateEvOrderExpired(string pfName, StockOrder expiredOrder)
        {
            string ev = PackGenFields(expiredOrder.LastDate, UserEventType.OrderExpired, UserEventMode.Unread, pfName, expiredOrder.STID);

            switch ( expiredOrder.Type )
            {
                case StockOrderType.Buy: ev += _unitSeparator + "OT=B"; break;
                case StockOrderType.Sell: ev += _unitSeparator + "OT=S"; break;
                default: ev += _unitSeparator + "OT=?"; break;
            }

            ev += string.Format("{0}TU={1}", _unitSeparator, expiredOrder.Units);
            ev += string.Format("{0}PU={1}", _unitSeparator, expiredOrder.PricePerUnit);

            return new(ev);
        }

        static public ClientUserEvent CreateEvOrderBuy(string pfName, DateTime date, StockOrder buyOrder)
        {
            string ev = PackGenFields(date, UserEventType.OrderBuy, UserEventMode.UnreadImp, pfName, buyOrder.STID);

            ev += _unitSeparator + "OT=B"; 
            ev += string.Format("{0}TU={1}", _unitSeparator, buyOrder.Units);
            ev += string.Format("{0}PU={1}", _unitSeparator, buyOrder.PricePerUnit);

            return new(ev);
        }

        static public ClientUserEvent CreateEvOrderSell(string pfName, DateTime date, StockOrder sellOrder)
        {
            string ev = PackGenFields(date, UserEventType.OrderSell, UserEventMode.UnreadImp, pfName, sellOrder.STID);

            ev += _unitSeparator + "OT=S";
            ev += string.Format("{0}TU={1}", _unitSeparator, sellOrder.Units);
            ev += string.Format("{0}PU={1}", _unitSeparator, sellOrder.PricePerUnit);

            return new(ev);
        }

        static public implicit operator StockOrder(ClientUserEvent ev)
        {
            try
            {
                return new()
                {
                    Units = decimal.Parse(ev.GetValue("TU")),
                    PricePerUnit = decimal.Parse(ev.GetValue("PU")),
                    STID = (Guid)ev,
                    Type = LocalGetOrderType(),
                };
            }
            catch (Exception)
            {

            }
            return null;

            StockOrderType LocalGetOrderType()
            {
                switch (ev.GetValue("OT"))
                {
                    case "B": return StockOrderType.Buy;
                    case "S": return StockOrderType.Sell;
                }
                return StockOrderType.Unknown;
            }
        }

        #endregion

        #region ALARM: Under / Over

        static public ClientUserEvent CreateEvAlarmUnder(Guid STID, StockAlarm alarm, decimal closed, decimal low, DateTime date)
        {
            // "Stock closed to xx.xx under alarm yy.yy"                                    // Later! Include note? its inside of alarm atm just not used atm
            // "Stock visited at xx.xx under alarm level yy.yy, but closed over to zz.zz"
            string ev = PackGenFields(date, UserEventType.AlarmUnder, UserEventMode.Unread, null, STID);

            ev += string.Format("{0}AV={1}", _unitSeparator, alarm.Value);    
            ev += string.Format("{0}DC={1}", _unitSeparator, closed);

            if ( closed > alarm.Value )
                // only momentarily under alarm trigger level
                ev += string.Format("{0}DL={1}", _unitSeparator, low);

            return new(ev);
        }

        static public ClientUserEvent CreateEvAlarmOver(Guid STID, StockAlarm alarm, decimal closed, decimal high, DateTime date)
        {
            string ev = PackGenFields(date, UserEventType.AlarmOver, UserEventMode.Unread, null, STID);

            ev += string.Format("{0}AV={1}", _unitSeparator, alarm.Value);
            ev += string.Format("{0}DC={1}", _unitSeparator, closed);
            
            if (closed < alarm.Value)
                // only momentarily over alarm trigger level
                ev += string.Format("{0}DH={1}", _unitSeparator, high);

            return new(ev);
        }

        static public implicit operator Alarm(ClientUserEvent ev)
        {
            try
            {
                Alarm alarm = new()
                {
                    AlarmValue = decimal.Parse(ev.GetValue("AV")),
                    DayClosed = decimal.Parse(ev.GetValue("DC")),
                    DayLow = null,
                    DayHigh = null,
                };

                string dl = ev.GetValue("DL");

                if (string.IsNullOrEmpty(dl) == false)
                    alarm.DayLow = decimal.Parse(dl);

                string dh = ev.GetValue("DH");

                if (string.IsNullOrEmpty(dh) == false)
                    alarm.DayHigh = decimal.Parse(dh);

                return alarm;
            }
            catch ( Exception )
            {

            }
            return null;
        }

        public class Alarm
        {
            public decimal AlarmValue { get; set; }
            public decimal DayClosed { get; set; }
            public decimal? DayLow { get; set; }
            public decimal? DayHigh { get; set; }
        }

        #endregion

        protected string GetValue(string param)
        {
            string field = _content.Split(_unitSeparator).Where(s => s.StartsWith(param + "=")).SingleOrDefault();

            if (field == null)
                return string.Empty;

            return field.Substring(param.Length + 1);
        }

        static protected string PackGenFields(DateTime date, UserEventType type, UserEventMode mode, string pfName, Guid STID)
        {
            /* 
             * D Date (210703)
             * M UserEventMode (read,unread,unreadImp,starred, etc)
             * P PortfolioName
             * S Stock (STID)
             * T UserEventType (buy,sell, etc)
             */
            string ret = _unitSeparator + "D=" + date.ToString("yyMMdd");

            ret += _unitSeparator + "M=" + ModeShortcuts.Single(t => t.Item1 == mode).Item2;

            ret += _unitSeparator + "T=" + TypeShortcuts.Single(t => t.Item1 == type).Item2;

            if (string.IsNullOrEmpty(pfName) == false) 
                ret += _unitSeparator + "P=" + pfName;

            if (STID != Guid.Empty)
                ret += _unitSeparator + "S=" + STID.ToString();

            return ret;
        }

        public void UpdateMode(UserEventMode mode)
        {
            string current = _unitSeparator + "M=" + GetValue("M") + _unitSeparator;
            string newone = _unitSeparator + "M=" + ModeShortcuts.Single(t => t.Item1 == mode).Item2 + _unitSeparator;
            _content = _content.Replace(current, newone);
        }

        protected readonly static ImmutableArray<Tuple<UserEventType, string>> TypeShortcuts = ImmutableArray.Create(new Tuple<UserEventType, string>[]
        {
            new Tuple<UserEventType, string>( UserEventType.OrderExpired,   "OE"),
            new Tuple<UserEventType, string>( UserEventType.OrderBuy,       "OB"),
            new Tuple<UserEventType, string>( UserEventType.OrderSell,      "OS"),
            new Tuple<UserEventType, string>( UserEventType.AlarmUnder,     "AU"),
            new Tuple<UserEventType, string>( UserEventType.AlarmOver,      "AO"),
        });

        protected readonly static ImmutableArray<Tuple<UserEventMode, string>> ModeShortcuts = ImmutableArray.Create(new Tuple<UserEventMode, string>[]
        {
            new Tuple<UserEventMode, string>( UserEventMode.Unread, "U"),
            new Tuple<UserEventMode, string>( UserEventMode.UnreadImp, "I"),
            new Tuple<UserEventMode, string>( UserEventMode.Read, "R"),
            new Tuple<UserEventMode, string>( UserEventMode.Starred, "S"),
        });
    }
}
