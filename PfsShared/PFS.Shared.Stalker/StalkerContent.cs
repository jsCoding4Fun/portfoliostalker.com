/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using PFS.Shared.Types;

namespace PFS.Shared.Stalker
{
    // Wraps StalkerData w textual-command-API that allows editing StalkerData
    public class StalkerContent : StalkerData
    {
        // Has ability to collect list of performed transactions, but requires activation
        protected List<string> _actions = null;

        // This is main interface to operate StalkerContent with Action cmdLine -commands, requires Named parameters
        public StalkerError DoAction(string cmdLine)
        {
            StalkerAction stalkerAction;
            StalkerError error = ParseAction(cmdLine, out stalkerAction);

            if (error != StalkerError.OK)
                return error;

            error = DoAction(stalkerAction);

            if (error == StalkerError.OK && _actions != null)
            {
                _actions.Add(cmdLine);
            }
            return error;
        }

        public static void DeepCopy(StalkerContent from, StalkerContent to) // Main issue w C# to C++ is that just cant get strong feeling what happens on "assembler" level, too automatic,
        {                                                                   // so just in case well do it this way.. even this is easy code to catch errors on future extentions :/
            StalkerData.DeepCopy(from, to);

            to._actions = new();
        }

        public void TrackActions()
        {
            if ( _actions == null )
                _actions = new();
        }

        public List<string> GetActions()
        {
            if (_actions == null)
                return null;

            // Not allowed to clean, just return copy
            return new List<string>(_actions);
        }

        static public StalkerError ParseAction(string cmdLine, out StalkerAction stalkerAction)
        {
            stalkerAction = null;

            List<string> cmdSegments = StalkerSplit.SplitLine(cmdLine);
            cmdSegments.RemoveAll(s => string.IsNullOrWhiteSpace(s) == true);

            int segmentID = 0;

            foreach (string segment in cmdSegments)
            {
                segmentID++;

                // First segment is always expected to be Operation-Element combo defining whats done and for what...
                if ( segmentID == 1 )
                {
                    string[] param1Split = segment.Split('-');

                    if (param1Split.Length != 2)
                        return StalkerError.FAIL;

                    StalkerOperation operation = (StalkerOperation)Enum.Parse(typeof(StalkerOperation), param1Split[0]);
                    StalkerElement element = (StalkerElement)Enum.Parse(typeof(StalkerElement), param1Split[1]);

                    stalkerAction = StalkerAction.Create(operation, element);

                    if (stalkerAction == null)
                        return StalkerError.NotSupported;

                    continue;
                }

                // Rest of segments are parameters, each and every one of them. If 'strictParamNaming' is set then each 
                // given parameter must be formatted w Name=Value (but then actually allows mixed order). If its not set,
                // then parameters must be given exactly correct order but Name=Value is optional, as can use plain Value.
                // RULE: All internal code should use Name=Value, but speed commands expects exact ordering wo names.

                StalkerError error;

                // In case of strict naming, we dont pass segmentID, as expecting segment to be Name=Value
                error = stalkerAction.SetParam(segment);

                if (error != StalkerError.OK)
                    return error;
            }

            // All parsing is done now, and should have all parameters set with acceptable value 
            if (stalkerAction.IsReady() == false)
                return StalkerError.MissingParameters;

            return StalkerError.OK;
        }

        protected StalkerError DoAction(StalkerAction stalkerAction)
        {
            switch ( stalkerAction.Element)
            {
                case StalkerElement.Stock:              // STOCK: Add / Edit / Delete / Set

                    switch (stalkerAction.Operation)
                    {
                        case StalkerOperation.Add:      // Add-Stock MarketID Ticker CompanyName Stock

                            return StockAdd(
                                (MarketID)stalkerAction.Param("MarketID"),
                                (string)stalkerAction.Param("Ticker"),
                                (string)stalkerAction.Param("CompanyName"),
                                (Guid)stalkerAction.Param("Stock"));

                        case StalkerOperation.Edit:     // Edit-Stock Ticker CompanyName Stock

                            return StockEdit(
                                (string)stalkerAction.Param("Ticker"),
                                (string)stalkerAction.Param("CompanyName"),
                                (Guid)stalkerAction.Param("Stock"));

                        case StalkerOperation.Delete:   // Delete-Stock Stock

                            return StockDelete(
                                (Guid)stalkerAction.Param("Stock"));

                        case StalkerOperation.Set:      // Set-Stock Stock [+LastEdit]
                            {
                                StalkerError error = StalkerError.OK;

                                // 'Set' is special extension command.. performing per parameter operations

                                if (error == StalkerError.OK && string.IsNullOrEmpty((string)stalkerAction.Param("+LastEdit")) == false )
                                {
                                    // [+LastEdit] is optional parameter used to update information of that field value for stock

                                    error = StockSetLastStockEdit(
                                        (Guid)stalkerAction.Param("Stock"),
                                        (DateTime)stalkerAction.Param("+LastEdit"));
                                }

                                return error;
                            }
                    }
                    break;

                case StalkerElement.Portfolio:          // PORTFOLIO: Add / Edit / Delete / Top

                    switch ( stalkerAction.Operation ) 
                    {
                        case StalkerOperation.Add:      // Add-Portfolio PfName

                            return PortfolioAdd( 
                                (string)stalkerAction.Param("PfName"));

                        case StalkerOperation.Edit:     // Edit-Portfolio PfCurrName PfNewName

                            return PortfolioEdit(
                                (string)stalkerAction.Param("PfCurrName"),
                                (string)stalkerAction.Param("PfNewName"));

                        case StalkerOperation.Delete:  // Delete-Portfolio PfName

                            return PortfolioDelete(
                                (string)stalkerAction.Param("PfName"));

                        case StalkerOperation.Top:

                            return PortfolioTop(        // Top-Portfolio PfName
                                (string)stalkerAction.Param("PfName"));
                    }
                    break;

                case StalkerElement.Group:              // STOCK GROUP: Add / Edit / Delete / Top / Follow / Unfollow

                    switch (stalkerAction.Operation)
                    {
                        case StalkerOperation.Add:      // Add-Group PfName SgName

                            return StockGroupAdd(
                                (string)stalkerAction.Param("PfName"),
                                (string)stalkerAction.Param("SgName"));

                        case StalkerOperation.Edit:     // Edit-Group SgCurrName SgNewName

                            return StockGroupEdit(
                                (string)stalkerAction.Param("SgCurrName"),
                                (string)stalkerAction.Param("SgNewName"));

                        case StalkerOperation.Delete:   // Delete-Group SgName

                            return StockGroupDelete(
                                (string)stalkerAction.Param("SgName"));

                        case StalkerOperation.Top:      // Top-Group SgName

                            return StockGroupTop(
                                (string)stalkerAction.Param("SgName"));

                        case StalkerOperation.Follow:   // Follow-Group SgName Stock

                            return StockGroupFollow(
                                (string)stalkerAction.Param("SgName"),
                                (Guid)stalkerAction.Param("Stock"));

                        case StalkerOperation.Unfollow: // Unfollow-Group SgName Stock

                            return StockGroupUnfollow(
                                (string)stalkerAction.Param("SgName"),
                                (Guid)stalkerAction.Param("Stock"));
                    }
                    break;

                case StalkerElement.Holding:            // HOLDING: Add, Edit, Delete

                    switch (stalkerAction.Operation)
                    {
                        case StalkerOperation.Add:      // Add-Holding PfName Stock Date Units Price Fee HoldingID Conversion ConversionTo Note

                            return HoldingAdd(
                                (string)stalkerAction.Param("PfName"),
                                (Guid)stalkerAction.Param("Stock"),
                                (DateTime)stalkerAction.Param("Date"),
                                (decimal)stalkerAction.Param("Units"),
                                (decimal)stalkerAction.Param("Price"),
                                (decimal)stalkerAction.Param("Fee"),
                                (string)stalkerAction.Param("HoldingID"),
                                (decimal)stalkerAction.Param("Conversion"),
                                (CurrencyCode)stalkerAction.Param("ConversionTo"),
                                (string)stalkerAction.Param("Note"));

                        case StalkerOperation.Edit:     // Edit-Holding HoldingID Date Units Price Fee Conversion ConversionTo Note

                            return HoldingEdit(
                                (string)stalkerAction.Param("HoldingID"),
                                (DateTime)stalkerAction.Param("Date"),
                                (decimal)stalkerAction.Param("Units"),
                                (decimal)stalkerAction.Param("Price"),
                                (decimal)stalkerAction.Param("Fee"),
                                (decimal)stalkerAction.Param("Conversion"),
                                (CurrencyCode)stalkerAction.Param("ConversionTo"),
                                (string)stalkerAction.Param("Note"));

                        case StalkerOperation.Delete:   // Delete-Holding HoldingID

                            return HoldingDelete(
                                (string)stalkerAction.Param("HoldingID"));
                    }
                    break;

                case StalkerElement.Trade:              // TRADE: Add, Delete

                    switch (stalkerAction.Operation)
                    {
                        case StalkerOperation.Add:      // Add-Trade PfName Stock Date Units Price Fee TradeID HoldingStrID Conversion ConversionTo

                            return TradeAdd(
                                (string)stalkerAction.Param("PfName"),
                                (Guid)stalkerAction.Param("Stock"),
                                (DateTime)stalkerAction.Param("Date"),
                                (decimal)stalkerAction.Param("Units"),
                                (decimal)stalkerAction.Param("Price"),
                                (decimal)stalkerAction.Param("Fee"),
                                (string)stalkerAction.Param("TradeID"),
                                (string)stalkerAction.Param("HoldingStrID"),
                                (decimal)stalkerAction.Param("Conversion"),
                                (CurrencyCode)stalkerAction.Param("ConversionTo"));

                        case StalkerOperation.Delete:   // Delete-Trade TradeID

                            return TradeDelete(
                                (string)stalkerAction.Param("TradeID"));
                    }
                    break;

                case StalkerElement.Order:              // ORDER: Add, Edit, Delete

                    switch (stalkerAction.Operation)
                    {
                        case StalkerOperation.Add:      // Add-Order PfName Type Stock Units Price FirstDate LastDate

                            return OrderAdd(
                                (string)stalkerAction.Param("PfName"),
                                (StockOrderType)stalkerAction.Param("Type"),
                                (Guid)stalkerAction.Param("Stock"),
                                (decimal)stalkerAction.Param("Units"),
                                (decimal)stalkerAction.Param("Price"),
                                (DateTime)stalkerAction.Param("FirstDate"),
                                (DateTime)stalkerAction.Param("LastDate"));

                        case StalkerOperation.Edit:      // Edit-Order PfName Type Stock EditedPrice Units Price FirstDate LastDate

                            return OrderEdit(
                                (string)stalkerAction.Param("PfName"),
                                (StockOrderType)stalkerAction.Param("Type"),
                                (Guid)stalkerAction.Param("Stock"),
                                (decimal)stalkerAction.Param("EditedPrice"),
                                (decimal)stalkerAction.Param("Units"),
                                (decimal)stalkerAction.Param("Price"),
                                (DateTime)stalkerAction.Param("FirstDate"),
                                (DateTime)stalkerAction.Param("LastDate"));

                        case StalkerOperation.Delete:   // Delete-Order PfName Stock Price

                            return OrderDelete(
                                (string)stalkerAction.Param("PfName"),
                                (Guid)stalkerAction.Param("Stock"),
                                (decimal)stalkerAction.Param("Price"));
                    }
                    break;

                case StalkerElement.Divident:           // DIVIDENT: Add, Delete

                    switch (stalkerAction.Operation)
                    {
                        case StalkerOperation.Add:      // Add-Divident PfName Stock Date Units PaymentPerUnit DividentID Conversion ConversionTo

                            return DividentAdd(
                                (string)stalkerAction.Param("PfName"),
                                (Guid)stalkerAction.Param("Stock"),
                                (DateTime)stalkerAction.Param("Date"),
                                (decimal)stalkerAction.Param("Units"),
                                (decimal)stalkerAction.Param("PaymentPerUnit"),
                                (string)stalkerAction.Param("DividentID"),
                                (decimal)stalkerAction.Param("Conversion"),
                                (CurrencyCode)stalkerAction.Param("ConversionTo"));

                        case StalkerOperation.Delete:   // Delete-Divident DividentID

                            return DividentDelete(
                                (string)stalkerAction.Param("DividentID"));
                    }
                    break;

                case StalkerElement.Alarm:              // ALARM: Add, Delete, Edit, DeleteAll
                                                        // teoretically ReplaceAll could be cmdline extension, or do it as DeleteAll ?? Not rush
                    switch (stalkerAction.Operation)
                    {
                        case StalkerOperation.Add:      // Add-Alarm Type Stock Value Param1 Note

                            return AlarmAdd(
                                (StockAlarmType)stalkerAction.Param("Type"),
                                (Guid)stalkerAction.Param("Stock"),
                                (decimal)stalkerAction.Param("Value"),
                                (decimal)stalkerAction.Param("Param1"),
                                (string)stalkerAction.Param("Note"));

                        case StalkerOperation.Delete:   // Delete-Alarm Stock Value

                            return AlarmDelete(
                                (Guid)stalkerAction.Param("Stock"),
                                (decimal)stalkerAction.Param("Value"));

                        case StalkerOperation.DeleteAll: // DeleteAll-Alarm Stock

                            return AlarmDeleteAll(
                                (Guid)stalkerAction.Param("Stock"));

                        case StalkerOperation.Edit:     // // Edit-Alarm Type Stock EditedValue Value Param1 Note

                            return AlarmEdit(
                                (StockAlarmType)stalkerAction.Param("Type"),
                                (Guid)stalkerAction.Param("Stock"),
                                (decimal)stalkerAction.Param("EditedValue"),
                                (decimal)stalkerAction.Param("Value"),
                                (decimal)stalkerAction.Param("Param1"),
                                (string)stalkerAction.Param("Note"));
                    }
                    break;
            }
            return StalkerError.NotSupported;
        }

        #region STOCK

        protected StalkerError StockAdd(MarketID marketID, string ticker, string companyName, Guid STID)
        {
            if (_stocks.SingleOrDefault(s => s.Meta.STID == STID) != null)
                return StalkerError.Duplicate;

            if (_stocks.SingleOrDefault(s => s.Meta.MarketID == marketID && s.Meta.Ticker == ticker.ToUpper()) != null)
                return StalkerError.Duplicate;

            _stocks.Add(new Stock()
            {
                Alarms = new(),
                Meta = new()
                {
                    MarketID = marketID,
                    Ticker = ticker.ToUpper(),
                    Name = companyName,
                    STID = STID,
                }
            });

            return StalkerError.OK;
        }

        protected StalkerError StockEdit(string ticker, string companyName, Guid STID)
        {
            Stock stock = StockRef(STID);

            if (stock == null)
                return StalkerError.InvalidReference;

            stock.Meta.Ticker = ticker;
            stock.Meta.Name = companyName;

            return StalkerError.OK;
        }

        protected StalkerError StockDelete(Guid STID)
        {
            Stock stock = StockRef(STID);

            if (stock == null)
                return StalkerError.InvalidReference;

            if (_stockGroups.Any(g => g.StocksSTIDs.Contains(STID)) == true ) // !!!LATER!!! This is over enforcing... just remove it from stock group??
                return StalkerError.CantDeleteNotEmpty;

            if (_portfolios.Any(p => p.StockHoldings.Where(h => h.STID == STID).ToList().Count() > 0) == true)
                // Pretty much stock comes immune to delete after has Holdings added to it (or close to immune)
                return StalkerError.CantDeleteNotEmpty;

            if (_portfolios.Any(p => p.StockOrders.Where(h => h.STID == STID).ToList().Count() > 0) == true)
                return StalkerError.CantDeleteNotEmpty;

            // !!!LATER!!! Needs to check also 'sold positions' records here...

            _stocks.RemoveAll(s => s.Meta.STID == STID);

            return StalkerError.OK;
        }

        protected StalkerError StockSetLastStockEdit(Guid STID, DateTime lastStockEdit)
        {
            Stock stock = StockRef(STID);

            if (stock == null)
                return StalkerError.InvalidReference;

            stock.LastStockEdit = lastStockEdit;

            return StalkerError.OK;
        }

        #endregion

        #region PORTFOLIO

        protected StalkerError PortfolioAdd(string name)
        {
            if (_portfolios.Where(x => x.Name.ToUpper() == name.ToUpper()).Any() == true)
                // No duplicate names allowed
                return StalkerError.Duplicate;

            _portfolios.Add(new Portfolio()
            {
                Name = name,
                StockHoldings = new List<StockHolding>(),
            });

            return StalkerError.OK;
        }

        protected StalkerError PortfolioEdit(string pfCurrName, string pfNewName)
        {
            Portfolio pf = PortfolioRef(pfCurrName);

            if ( pf == null)
                return StalkerError.InvalidReference;

            // Carefull! PfName is used as ID, so renaming it requires renaming all references to it
            List<StockGroup> pfSgs = StockGroups().Where(g => g.OwnerPfName == pf.Name).ToList();

            foreach ( StockGroup sg in pfSgs )
                sg.OwnerPfName = pfNewName;

            // Finally rename PF itself
            pf.Name = pfNewName;

            return StalkerError.OK;
        }

        protected StalkerError PortfolioDelete(string pfName)
        {
            Portfolio pf = PortfolioRef(pfName);

            if (pf == null)
                return StalkerError.InvalidReference;

            ReadOnlyCollection<StockHolding> holdings = PortfolioHoldings(pfName);

            if (holdings.Count > 0)
                return StalkerError.CantDeleteNotEmpty;

            ReadOnlyCollection<StockTrade> trades = PortfolioTrades(pfName);

            if (trades.Count > 0)
                return StalkerError.CantDeleteNotEmpty;

            ReadOnlyCollection<StockGroup> groups = StockGroups(pfName);

            if (groups.Count > 0)
                return StalkerError.CantDeleteNotEmpty;

            _portfolios.RemoveAll(p => p.Name == pfName);

            return StalkerError.OK;
        }

        protected StalkerError PortfolioTop(string pfName)
        {
            Portfolio pf = PortfolioRef(pfName);

            if (pf == null)
                return StalkerError.InvalidReference;

            int position = _portfolios.IndexOf(pf);

            _portfolios.RemoveAt(position);
            _portfolios.Insert(0, pf);

            return StalkerError.OK;
        }

        #endregion

        #region STOCK GROUP

        protected StalkerError StockGroupAdd(string pfName, string sgName)
        {
            if (PortfolioRef(pfName) == null)
                return StalkerError.InvalidReference;

            if (_stockGroups.Where(x => x.Name.ToUpper() == sgName.ToUpper()).Any() == true)
                // No duplicate names allowed, not even under different portfolios
                return StalkerError.Duplicate;

            _stockGroups.Add(new StockGroup()
            {
                Name = sgName,
                OwnerPfName = pfName,
                StocksSTIDs = new(),
            });

            return StalkerError.OK;
        }

        protected StalkerError StockGroupEdit(string sgCurrName, string sgNewName)
        {
            StockGroup sg = StockGroupRef(sgCurrName);

            if (sg == null)
                return StalkerError.InvalidReference;

            sg.Name = sgNewName;

            return StalkerError.OK;
        }

        protected StalkerError StockGroupDelete(string sgName)
        {
            StockGroup sg = StockGroupRef(sgName);

            if (sg == null)
                return StalkerError.InvalidReference;

            _stockGroups.RemoveAll(p => p.Name == sgName);

            return StalkerError.OK;
        }

        protected StalkerError StockGroupTop(string sgName)
        {
            StockGroup sg = StockGroupRef(sgName);

            if (sg == null)
                return StalkerError.InvalidReference;

            int position = _stockGroups.IndexOf(sg);

            _stockGroups.RemoveAt(position);
            _stockGroups.Insert(0, sg);

            return StalkerError.OK;
        }

        protected StalkerError StockGroupFollow(string sgName, Guid STID)
        {
            StockGroup stockGroup = StockGroupRef(sgName);

            if (stockGroup == null)
                return StalkerError.InvalidReference;

            if (stockGroup.StocksSTIDs.Any(s => s == STID) == true)
                // Yeah could return error, but really same to return OK as its already there
                return StalkerError.OK;

            stockGroup.StocksSTIDs.Add(STID);
            return StalkerError.OK;
        }

        protected StalkerError StockGroupUnfollow(string sgName, Guid STID)
        {
            StockGroup stockGroup = StockGroupRef(sgName);

            if (stockGroup == null)
                return StalkerError.InvalidReference;

            stockGroup.StocksSTIDs.RemoveAll(s => s == STID);

            return StalkerError.OK;
        }

        #endregion

        #region ALARMS

        protected StalkerError AlarmAdd(StockAlarmType Type, Guid STID, decimal value, decimal param, string note)
        {
            Stock stock = StockRef(STID);

            if (stock == null)
                return StalkerError.UnTrackedStock;

            if (stock.Alarms.SingleOrDefault(a => a.Value == value) != null)
                // Multiple alarms w same Value level for one stock are NOT allowed, not even different types
                return StalkerError.Duplicate;

            stock.Alarms.Add(new StockAlarm()
            {
                Type = Type,
                Value = value,
                Param1 = param,
                Note = note,
            });

            return StalkerError.OK;
        }

        protected StalkerError AlarmDelete(Guid STID, decimal value)
        {
            Stock stock = StockRef(STID);

            if (stock == null)
                return StalkerError.UnTrackedStock;

            stock.Alarms.RemoveAll(a => a.Value == value);

            return StalkerError.OK;
        }

        protected StalkerError AlarmDeleteAll(Guid STID)
        {
            Stock stock = StockRef(STID);

            if (stock == null)
                return StalkerError.UnTrackedStock;

            stock.Alarms = new();

            return StalkerError.OK;
        }

        protected StalkerError AlarmEdit(StockAlarmType Type, Guid STID, decimal editedValue, decimal value, decimal param, string note)
        {
            Stock stock = StockRef(STID);

            if (stock == null)
                return StalkerError.UnTrackedStock;

            StockAlarm alarm = stock.Alarms.SingleOrDefault(a => a.Value == editedValue);

            if ( alarm == null )
                // Multiple alarms w same Value level for one stock are NOT allowed, not even different types
                return StalkerError.InvalidReference;

            alarm.Type = Type;
            alarm.Value = value;
            alarm.Param1 = param;
            alarm.Note = note; 

            return StalkerError.OK;
        }

        #endregion

        #region HOLDINGS

        internal StalkerError HoldingSetRemaining(string holdingID, decimal remainingUnits)
        {
            /* Basic StalkerApi doesnt allow editing of 'RemainingUnits', but XML needs to be able to set it correctly on Import.
             * So this INTERNAL function HoldingSetRemaining allows things Imported back to correct state.
             */

            Portfolio pf = _portfolios.Where(p => p.StockHoldings.Any(h => h.HoldingID == holdingID)).SingleOrDefault();

            if (pf == null)
                return StalkerError.InvalidReference;

            StockHolding holding = pf.StockHoldings.SingleOrDefault(h => h.HoldingID == holdingID);

            if (holding == null)
                return StalkerError.FAIL;

            holding.RemainingUnits = remainingUnits;

            return StalkerError.OK;
        }

        internal StalkerError HoldingSetDividents(string holdingID, List<StockHolding.DividentsToHolding> dividents)
        {
            Portfolio pf = _portfolios.Where(p => p.StockHoldings.Any(h => h.HoldingID == holdingID)).SingleOrDefault();

            if (pf == null)
                return StalkerError.InvalidReference;

            StockHolding holding = pf.StockHoldings.SingleOrDefault(h => h.HoldingID == holdingID);

            if (holding == null)
                return StalkerError.FAIL;

            holding.Dividents = dividents;

            return StalkerError.OK;
        }

        protected StalkerError HoldingAdd(string pfName, Guid STID, DateTime purhaceDate, decimal units, decimal pricePerUnit, 
                                          decimal fee, string holdingID, decimal conversion, CurrencyCode conversionTo, string note)
        {
            Portfolio pf = PortfolioRef(pfName);

            if (pf == null)
                return StalkerError.InvalidReference;

            if (string.IsNullOrWhiteSpace(holdingID) == true)
                return StalkerError.InvalidParameter;

            if (_portfolios.Any(p => p.StockHoldings.Any(h => h.HoldingID == holdingID)))
                return StalkerError.Duplicate;

            pf.StockHoldings.Add(new StockHolding()
            {
                STID = STID,
                PurhacedUnits = units,
                PricePerUnit = pricePerUnit,
                RemainingUnits = units,
                PurhaceDate = purhaceDate,
                Fee = fee,
                HoldingID = holdingID,
                ConversionRate = conversion,
                ConversionTo = conversionTo,
                HoldingNote = note,
            });

            return StalkerError.OK;
        }

        protected StalkerError HoldingEdit(string holdingID, DateTime purhaceDate, decimal units, decimal pricePerUnit, decimal fee, 
                                           decimal conversion, CurrencyCode conversionTo, string note)
        {
            if (string.IsNullOrWhiteSpace(holdingID) == true)
                return StalkerError.InvalidParameter;

            Portfolio pf = _portfolios.Where(p => p.StockHoldings.Any(h => h.HoldingID == holdingID)).SingleOrDefault();

            if (pf == null)
                return StalkerError.InvalidReference;

            StockHolding holding = pf.StockHoldings.SingleOrDefault(h => h.HoldingID == holdingID);

            if (holding == null)
                return StalkerError.FAIL;

            if (holding.RemainingUnits != holding.PurhacedUnits)
                // Has part of this traded/sold cant do editing anymore
                return StalkerError.NotSupported;

            holding.PurhaceDate = purhaceDate;
            holding.PurhacedUnits = units;
            holding.RemainingUnits = units;
            holding.PricePerUnit = pricePerUnit;
            holding.Fee = fee;
            holding.ConversionRate = conversion;
            holding.ConversionTo = conversionTo;
            holding.HoldingNote = note;

            return StalkerError.OK;
        }

        protected StalkerError HoldingDelete(string holdingID)
        {
            if (string.IsNullOrWhiteSpace(holdingID) == true)
                return StalkerError.InvalidParameter;

            Portfolio pf = _portfolios.Where(p => p.StockHoldings.Any(h => h.HoldingID == holdingID)).SingleOrDefault();

            if (pf == null)
                return StalkerError.InvalidReference;

            StockHolding holding = pf.StockHoldings.SingleOrDefault(h => h.HoldingID == holdingID);

            if (holding == null)
                return StalkerError.FAIL;

            if (holding.RemainingUnits != holding.PurhacedUnits)
                // Has part of this traded/sold cant do delete anymore
                return StalkerError.NotSupported;

            pf.StockHoldings.RemoveAll(h => h.HoldingID == holdingID);

            return StalkerError.OK;
        }

        #endregion

        #region TRADES / SALES

        internal StalkerError TradeCreate(string pfName, Guid STID, DateTime saleDate, decimal units, decimal pricePerUnit,
                                          decimal fee, string tradeID, decimal conversion, CurrencyCode conversionTo,
                                          List<StockTrade.SoldHoldingType> holdings)
        {
            /* As logic with TradeAdd is bit more tuff than avarage function, reason being that it makes FIFO functionalities etc.
             * This basic TradeAdd is pretty much unusable for StalkerXML Importing, that as far as possible is attempting to use
             * same Stalker DoAction API as application itself... but in this case we cant rerun this FIFO logic on XML import and
             * honesly expect to have same output every time.. 
             * 
             * => Functionality is divided so that 'TradeAdd' is one that is used from application, and doing decision logic to 
             *    pick holdings it sells on FIFO case... as for end application we want interface to be easy to use...
             *    
             * => On XML Import 'TradeAdd' function is NOT used, but StockHolding loads itself properly, and as a replacement to TradeAdd
             *    a import calls internal function 'TradeCreate' that allows to import exact same state that was stored by providing
             *    list of holdings those where changed by trade
             */

            Portfolio pf = PortfolioRef(pfName);

            if (pf == null)
                return StalkerError.InvalidReference;

            if (string.IsNullOrWhiteSpace(tradeID) == true)
                return StalkerError.InvalidParameter;

            if (_portfolios.Any(p => p.StockTrades.Any(t => t.TradeID == tradeID)) == true)
                return StalkerError.Duplicate;

            StockTrade trade = new StockTrade()
            {
                STID = STID,
                TradeID = tradeID,
                SoldUnits = units,
                PricePerUnit = pricePerUnit,
                Fee = fee,
                SaleDate = saleDate,
                ConversionRate = conversion,
                ConversionTo = conversionTo,
                SoldHoldings = holdings,
            };

            pf.StockTrades.Add(trade);

            return StalkerError.OK;
        }

        protected StalkerError TradeAdd(string pfName, Guid STID, DateTime saleDate, decimal units, decimal pricePerUnit,
                                        decimal fee, string tradeID, string holdingStrID, decimal conversion, CurrencyCode conversionTo)
        {
            StalkerError ret = StalkerError.OK;

            Portfolio pf = PortfolioRef(pfName);

            if (pf == null)
                return StalkerError.InvalidReference;

            if (string.IsNullOrWhiteSpace(tradeID) == true)
                return StalkerError.InvalidParameter;

            if (_portfolios.Any(p => p.StockTrades.Any(t => t.TradeID == tradeID)) == true)
                return StalkerError.Duplicate;

            StockTrade trade = new StockTrade()
            {
                STID = STID,
                TradeID = tradeID,
                SoldUnits = units,
                PricePerUnit = pricePerUnit,
                Fee = fee,
                SaleDate = saleDate,
                ConversionRate = conversion,
                ConversionTo = conversionTo,
                SoldHoldings = new(),
            };

            if (string.IsNullOrEmpty(holdingStrID) == true)
                // 'holdingStrID' acts as optional parameter, needs to be given, but if empty then does default First-In-First-Out selection
                ret = LocalSaleAsFIFO();
            else 
                ret = LocalSaleSpecificHolding();

            if ( ret == StalkerError.OK )
                pf.StockTrades.Add(trade);

            return ret;


            StalkerError LocalSaleAsFIFO()
            {
                decimal available = pf.StockHoldings.Where(h => h.RemainingUnits > 0 && h.STID == STID).ToList().ConvertAll(h => h.RemainingUnits).Sum();

                if (available < units)
                    return StalkerError.SellingUnitsMoreThanOwns;

                List<StockHolding> allHoldings = pf.StockHoldings.Where(h => h.RemainingUnits > 0 && h.STID == STID).OrderBy(h => h.PurhaceDate).ToList();

                decimal unitsLeftToSale = units;

                // Loop remaining holdings for stock under this portfolio, in FIFO order and 'sell' holdings until required amount done
                foreach (StockHolding holding in allHoldings )
                {
                    if ( holding.RemainingUnits >= unitsLeftToSale)
                    {
                        trade.SoldHoldings.Add(new()
                        {
                            HoldingID = holding.HoldingID,
                            SoldUnits = unitsLeftToSale,
                        });

                        holding.RemainingUnits -= unitsLeftToSale;
                        return StalkerError.OK;
                    }
                    
                    trade.SoldHoldings.Add(new()
                    {
                        HoldingID = holding.HoldingID,
                        SoldUnits = holding.RemainingUnits,
                    });

                    unitsLeftToSale -= holding.RemainingUnits;
                    holding.RemainingUnits = 0;
                }
                // Should never come here as already checked that there is enough holdings for this sale...
                return StalkerError.FAIL;
            }

            StalkerError LocalSaleSpecificHolding()
            {
                StockHolding holding = pf.StockHoldings.SingleOrDefault(h => h.HoldingID == holdingStrID);

                if (holding == null || holding.RemainingUnits < units)
                    return StalkerError.InvalidReference;

                // On 'StockHolding' we only reduce 'RemainingUnits'
                holding.RemainingUnits -= units;

                // On 'StockTrade' side we track specific holding that was sold to close this Trade
                trade.SoldHoldings.Add(new()
                {
                     HoldingID = holding.HoldingID,
                     SoldUnits = units,
                });

                return StalkerError.OK;
            }
        }

        protected StalkerError TradeDelete(string tradeID)
        {
            /* !!!THINK!!! Generally with many holdings, and TradeAdd's there is lot of potential issues w automatic FIFO base functionality.
             *             This really makes TradeDelete something that user has to be very carefull or can easily start messing up order of
             *             FIFO and make things off sync. Sadly cant see easy way limiting it here, as enforcing it requires enforcing
             *             holding add/edit orders and so many other hard to test limitiations.
             *             
             *             => Atm this is almost too powerfull to have, but then dont have edit so user must be able to do something to fix errors.
             */


            if (string.IsNullOrWhiteSpace(tradeID) == true)
                return StalkerError.InvalidParameter;

            Portfolio pf = _portfolios.Where(p => p.StockTrades.Any(h => h.TradeID == tradeID)).SingleOrDefault();

            if (pf == null)
                return StalkerError.InvalidReference;

            StockTrade trade = pf.StockTrades.SingleOrDefault(t => t.TradeID == tradeID);

            if (trade == null)
                return StalkerError.FAIL;

            foreach ( StockTrade.SoldHoldingType soldHolding in trade.SoldHoldings )
            {
                StockHolding holding = pf.StockHoldings.Single(h => h.HoldingID == soldHolding.HoldingID);
                holding.RemainingUnits += soldHolding.SoldUnits;
            }

            pf.StockTrades.RemoveAll(h => h.TradeID == tradeID);

            return StalkerError.OK;
        }

        #endregion

        #region ORDERS

        protected StalkerError OrderAdd(string pfName, StockOrderType type, Guid STID, decimal units, decimal pricePerUnit, DateTime firstDate, DateTime lastDate)
        {
            Portfolio pf = PortfolioRef(pfName);

            if (pf == null)
                return StalkerError.InvalidReference;

            if (pf.StockOrders.SingleOrDefault(a => a.PricePerUnit == pricePerUnit && a.STID == STID) != null)
                // Multiple orders w same Price for one specific stock are NOT allowed, not even different types (as pricePerUnit is used as reference ID)
                return StalkerError.Duplicate;

            pf.StockOrders.Add(new StockOrder()
            {
                STID = STID,
                Type = type,
                Units = units,
                PricePerUnit = pricePerUnit,
                FirstDate = firstDate,
                LastDate = lastDate,
            });

            return StalkerError.OK;
        }

        protected StalkerError OrderEdit(string pfName, StockOrderType type, Guid STID, decimal editedPrice, decimal units, decimal pricePerUnit, DateTime firstDate, DateTime lastDate)
        {
            Portfolio pf = PortfolioRef(pfName);

            if (pf == null)
                return StalkerError.InvalidReference;

            // 'PricePerUnit' is used as ID to refer specific order under portfolio/stock
            StockOrder order = pf.StockOrders.SingleOrDefault(a => a.PricePerUnit == editedPrice && a.STID == STID);

            if (order == null)
                return StalkerError.InvalidReference;

            order.Type = type;
            order.Units = units;
            order.PricePerUnit = pricePerUnit;
            order.FirstDate = firstDate;
            order.LastDate = lastDate;

            return StalkerError.OK;
        }

        protected StalkerError OrderDelete(string pfName, Guid STID, decimal pricePerUnit)
        {
            Portfolio pf = PortfolioRef(pfName);

            if (pf == null)
                return StalkerError.InvalidReference;

            if (pf.StockOrders.SingleOrDefault(a => a.PricePerUnit == pricePerUnit && a.STID == STID) == null)
                // Doesnt exist
                return StalkerError.InvalidReference;

            pf.StockOrders.RemoveAll(a => a.PricePerUnit == pricePerUnit && a.STID == STID);

            return StalkerError.OK;
        }

        #endregion

        #region DIVIDENTS

        internal StalkerError DividentCreate(string pfName, Guid STID, DateTime date, decimal units, decimal paymentPerUnit,
                                             string dividentID, decimal conversion, CurrencyCode conversionTo)
        {
            Portfolio pf = PortfolioRef(pfName);

            if (pf == null)
                return StalkerError.InvalidReference;

            if (string.IsNullOrWhiteSpace(dividentID) == true)
                return StalkerError.InvalidParameter;

            if (_portfolios.Any(p => p.StockDividents.Any(d => d.DividentID == dividentID)))
                return StalkerError.Duplicate;

            pf.StockDividents.Add(new StockDivident()
            {
                STID = STID,
                Units = units,
                PaymentPerUnit = paymentPerUnit,
                Date = date,
                DividentID = dividentID,
                ConversionRate = conversion,
                ConversionTo = conversionTo,
            });

            return StalkerError.OK;
        }

        protected StalkerError DividentAdd(string pfName, Guid STID, DateTime date, decimal units, decimal paymentPerUnit,
                                           string dividentID, decimal conversion, CurrencyCode conversionTo)
        {
            StalkerError err = DividentCreate(pfName, STID, date, units, paymentPerUnit, dividentID, conversion, conversionTo);

            if (err != StalkerError.OK)
                return err;

            Portfolio pf = PortfolioRef(pfName);

            LocalSplitDividentToHoldings(pf, STID, units, dividentID);

            return StalkerError.OK;


            void LocalSplitDividentToHoldings(Portfolio pf, Guid STID, decimal units, string dividentID)
            {
                List<StockHolding> holdings = pf.StockHoldings.Where(h => h.STID == STID && h.RemainingUnits > 0).ToList();
                decimal remainingUnits = units;
                
                foreach ( StockHolding holding in holdings )
                {
                    if ( holding.RemainingUnits <= remainingUnits )
                    {
                        holding.Dividents.Add(new()
                        {
                            DividentID = dividentID,
                            Units = holding.RemainingUnits,
                        });
                    }
                    else
                    {
                        // !!!LATER!!! If units left to assign is less than RemainingUnits, something is generally wrong.. and should not assign any?
                    }

                    remainingUnits -= holding.RemainingUnits;
                }
            }
        }

        protected StalkerError DividentDelete(string dividentID)
        {
            if (string.IsNullOrWhiteSpace(dividentID) == true)
                return StalkerError.InvalidParameter;

            Portfolio pf = _portfolios.Where(p => p.StockDividents.Any(d => d.DividentID == dividentID)).SingleOrDefault();

            if (pf == null)
                return StalkerError.InvalidReference;

            StockDivident divident = pf.StockDividents.SingleOrDefault(d => d.DividentID == dividentID);

            if (divident == null)
                return StalkerError.FAIL;

            pf.StockHoldings.Where(s => s.STID == divident.STID).ToList().ForEach(h => h.Dividents.RemoveAll(d => d.DividentID == dividentID));

            pf.StockDividents.Remove(divident);

            return StalkerError.OK;
        }

        #endregion
    }
}
