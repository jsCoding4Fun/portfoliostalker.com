/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Serilog;

using PFS.Shared.Types;

namespace PFS.Shared.Stalker
{
    public class StalkerXML
    {
        // 
        public static string ExportXml(StalkerData stalkerData)
        {
            XElement rootPFS = new XElement("PFS");

            // PORTFOLIO

            XElement allPfElem = new XElement("Portfolios");
            rootPFS.Add(allPfElem);

            foreach (Portfolio pf in stalkerData.Portfolios())
            {
                XElement myPfElem = new XElement("Portfolio");
                myPfElem.SetAttributeValue("Name", pf.Name);
                allPfElem.Add(myPfElem);

                // portfolios: Stock Groups

                XElement pfSgElem = new XElement("StockGroups");
                myPfElem.Add(pfSgElem);

                foreach (StockGroup sg in stalkerData.StockGroups(pf.Name))
                {
                    XElement mySgElem = new XElement("StockGroup");
                    mySgElem.SetAttributeValue("Name", sg.Name);
                    pfSgElem.Add(mySgElem);

                    foreach (Guid STID in sg.StocksSTIDs)
                    {
                        Stock stock = stalkerData.StockRef(STID);

                        XElement mySgStElem = new XElement("Stock");
                        mySgStElem.SetAttributeValue("Market", stock.Meta.MarketID.ToString());
                        mySgStElem.SetAttributeValue("Ticker", stock.Meta.Ticker);
                        mySgStElem.SetAttributeValue("STID", STID);
                        mySgElem.Add(mySgStElem);
                    }
                }

                // portfolios: Stock Orders

                XElement pfOrElem = new XElement("StockOrders");
                myPfElem.Add(pfOrElem);

                foreach (StockOrder order in stalkerData.PortfolioOrders(pf.Name))
                {
                    Stock stock = stalkerData.StockRef(order.STID);

                    XElement myPfOrElem = new XElement(order.Type.ToString());
                    myPfOrElem.SetAttributeValue("Market", stock.Meta.MarketID.ToString());
                    myPfOrElem.SetAttributeValue("Ticker", stock.Meta.Ticker);
                    myPfOrElem.SetAttributeValue("STID", order.STID);

                    myPfOrElem.SetAttributeValue("Units", order.Units);
                    myPfOrElem.SetAttributeValue("Price", order.PricePerUnit);

                    myPfOrElem.SetAttributeValue("FDate", order.FirstDate.ToString("yyyy-MM-dd"));
                    myPfOrElem.SetAttributeValue("LDate", order.LastDate.ToString("yyyy-MM-dd"));

                    pfOrElem.Add(myPfOrElem);
                }

                // portfolios: Stock Holdings

                XElement pfShElem = new XElement("StockHoldings");
                myPfElem.Add(pfShElem);

                foreach (StockHolding holding in stalkerData.PortfolioHoldings(pf.Name, true))
                {
                    Stock stock = stalkerData.StockRef(holding.STID); 

                    XElement myPfShElem = new XElement("StockHolding");
                    myPfShElem.SetAttributeValue("Market", stock.Meta.MarketID.ToString());
                    myPfShElem.SetAttributeValue("Ticker", stock.Meta.Ticker);
                    myPfShElem.SetAttributeValue("STID", holding.STID);

                    myPfShElem.SetAttributeValue("PUnits", holding.PurhacedUnits);
                    myPfShElem.SetAttributeValue("Price", holding.PricePerUnit);
                    myPfShElem.SetAttributeValue("Fee", holding.Fee);
                    myPfShElem.SetAttributeValue("PDate", holding.PurhaceDate.ToString("yyyy-MM-dd"));
                    myPfShElem.SetAttributeValue("RUnits", holding.RemainingUnits);
                    myPfShElem.SetAttributeValue("HoldingID", holding.HoldingID);
                    myPfShElem.SetAttributeValue("Note", holding.HoldingNote);

                    if (holding.ConversionRate != 0)
                    {
                        myPfShElem.SetAttributeValue("Conversion", holding.ConversionRate);

                        if (holding.ConversionTo != CurrencyCode.Unknown)
                            myPfShElem.SetAttributeValue("ConversionTo", holding.ConversionTo.ToString());
                    }

                    foreach ( StockHolding.DividentsToHolding divident in holding.Dividents )
                    {
                        XElement myShDivElem = new XElement("Divident");
                        myShDivElem.SetAttributeValue("Units", divident.Units);
                        myShDivElem.SetAttributeValue("ID", divident.DividentID);
                        myPfShElem.Add(myShDivElem);
                    }

                    pfShElem.Add(myPfShElem);
                }

                // portfolios: Stock Dividents

                XElement pfDiElem = new XElement("StockDividents");
                myPfElem.Add(pfDiElem);

                foreach (StockDivident divident in stalkerData.PortfolioDividents(pf.Name))
                {
                    Stock stock = stalkerData.StockRef(divident.STID);

                    XElement myPfDiElem = new XElement("StockDivident");
                    myPfDiElem.SetAttributeValue("Market", stock.Meta.MarketID.ToString());
                    myPfDiElem.SetAttributeValue("Ticker", stock.Meta.Ticker);
                    myPfDiElem.SetAttributeValue("STID", divident.STID);

                    myPfDiElem.SetAttributeValue("Units", divident.Units);
                    myPfDiElem.SetAttributeValue("PaymentPerUnit", divident.PaymentPerUnit);
                    myPfDiElem.SetAttributeValue("Date", divident.Date.ToString("yyyy-MM-dd"));
                    myPfDiElem.SetAttributeValue("DividentID", divident.DividentID);

                    if (divident.ConversionRate != 0)
                    {
                        myPfDiElem.SetAttributeValue("Conversion", divident.ConversionRate);

                        if (divident.ConversionTo != CurrencyCode.Unknown)
                            myPfDiElem.SetAttributeValue("ConversionTo", divident.ConversionTo.ToString());
                    }

                    pfDiElem.Add(myPfDiElem);
                }

                // portfolios: Stock Trades

                XElement pfStElem = new XElement("StockTrades");
                myPfElem.Add(pfStElem);

                foreach (StockTrade trade in stalkerData.PortfolioTrades(pf.Name))
                {
                    Stock stock = stalkerData.StockRef(trade.STID);

                    XElement myPfStElem = new XElement("StockTrade");
                    myPfStElem.SetAttributeValue("Market", stock.Meta.MarketID.ToString());
                    myPfStElem.SetAttributeValue("Ticker", stock.Meta.Ticker);
                    myPfStElem.SetAttributeValue("STID", trade.STID);

                    myPfStElem.SetAttributeValue("SUnits", trade.SoldUnits);
                    myPfStElem.SetAttributeValue("Price", trade.PricePerUnit);
                    myPfStElem.SetAttributeValue("Fee", trade.Fee);
                    myPfStElem.SetAttributeValue("SDate", trade.SaleDate.ToString("yyyy-MM-dd"));
                    myPfStElem.SetAttributeValue("TradeID", trade.TradeID);

                    if (trade.ConversionRate != 0)
                    {
                        myPfStElem.SetAttributeValue("Conversion", trade.ConversionRate);

                        if (trade.ConversionTo != CurrencyCode.Unknown)
                            myPfStElem.SetAttributeValue("ConversionTo", trade.ConversionTo.ToString());
                    }

                    foreach (StockTrade.SoldHoldingType holding in trade.SoldHoldings )
                    {
                        XElement myTradeHoldingElem = new XElement("SoldHolding");
                        myTradeHoldingElem.SetAttributeValue("HoldingID", holding.HoldingID);
                        myTradeHoldingElem.SetAttributeValue("SoldUnits", holding.SoldUnits);
                        myPfStElem.Add(myTradeHoldingElem);
                    }

                    pfStElem.Add(myPfStElem);
                }
            }

            // STOCK's

            XElement allStElem = new XElement("Stocks");
            rootPFS.Add(allStElem);

            foreach (Stock stock in stalkerData.Stocks())
            {
                XElement myStElem = new XElement("Stock");
                myStElem.SetAttributeValue("Market", stock.Meta.MarketID.ToString());
                myStElem.SetAttributeValue("Ticker", stock.Meta.Ticker);
                myStElem.SetAttributeValue("Name", stock.Meta.Name);
                myStElem.SetAttributeValue("STID", stock.Meta.STID);
                myStElem.SetAttributeValue("LastEdit", stock.LastStockEdit.ToString("yyyy-MM-dd"));
                allStElem.Add(myStElem);

                if (stock.Alarms.Count() > 0)
                {
                    XElement stStockAlarms = new XElement("StockAlarms");
                    myStElem.Add(stStockAlarms);

                    foreach (StockAlarm alarm in stock.Alarms)
                    {
                        XElement myStSaElem = new XElement(alarm.Type.ToString());
                        myStSaElem.SetAttributeValue("Value", alarm.Value.ToString());
                        myStSaElem.SetAttributeValue("Note", alarm.Note);
                        myStSaElem.SetAttributeValue("Param1", alarm.Param1);
                        stStockAlarms.Add(myStSaElem);
                    }
                }
            }

            return rootPFS.ToString();
        }

        // As atm 'stalkerContent' is expected to be empty when calling this, as merge case is not tested 
        public static StalkerError ImportXml(StalkerContent stalkerContent, string xml)
        {
            StalkerError error = StalkerError.FAIL;
            string cmd = string.Empty;

            /* !!!DECISION!!!: ImportXML that is way Backups are imported account needs to be using StalkerAPI as 
             *                 this is also way server may accesses information from client... and servers case 
             *                 these protections offered by StalkerAPI are important to prevent crashing and even
             *                 more so to prevent illegal syntax been entered to database etc
             */

            try
            {
                XDocument xmlDoc = XDocument.Parse(xml);
                XElement rootPFS = xmlDoc.Descendants("PFS").First();

                // Import Stock's so that StockMeta information gets setup 

                foreach (XElement myStElem in rootPFS.Element("Stocks").Descendants("Stock"))
                {
                    // <Stock Market="NYSE" Ticker="T" Name="AT&amp;T Inc." STID="f10624d1-c3b5-43ea-914f-ef0e5395598a">

                    StockMeta stockMeta = new()
                    {
                        STID = (Guid)myStElem.Attribute("STID"),
                        MarketID = (MarketID)Enum.Parse(typeof(MarketID), (string)myStElem.Attribute("Market")),
                        Ticker = (string)myStElem.Attribute("Ticker"),
                        Name = (string)myStElem.Attribute("Name"),
                    };

                    // Per StalkerActionTemplate:  Add-Stock MarketID Ticker CompanyName Stock
                    cmd = string.Format("Add-Stock MarketID=[{0}] Ticker=[{1}] CompanyName=[{2}] Stock=[{3}]",
                                        stockMeta.MarketID.ToString(), stockMeta.Ticker, stockMeta.Name, stockMeta.STID);
                    
                    if ((error = stalkerContent.DoAction(cmd)) != StalkerError.OK)
                        throw new Exception("Failed to handle Add-Stock per PFS.Stocks.Stock -list");

                    if (myStElem.Attribute("LastEdit") != null)     // !!!TODO!!! !!!LATER!!! Expect this to be there, but atm transition so cant allow crash..
                    {
                        DateTime lastEdit = (DateTime)myStElem.Attribute("LastEdit");

                        cmd = string.Format("Set-Stock Stock=[{0}] +LastEdit=[{1}]", stockMeta.STID, lastEdit.ToString("yyyy-MM-dd"));

                        if ((error = stalkerContent.DoAction(cmd)) != StalkerError.OK)
                            throw new Exception("Failed to handle Set-Stock per PFS.Stocks.Stock -list");
                    }

                    if (myStElem.Descendants("StockAlarms") != null)
                    {
                        foreach (XElement myStSaElem in myStElem.Descendants("StockAlarms").Descendants())
                        {
                            StockAlarmType alarmType = (StockAlarmType)Enum.Parse(typeof(StockAlarmType), myStSaElem.Name.ToString());
                            decimal alarmValue = (decimal)myStSaElem.Attribute("Value");
                            string alarmNote = (string)myStSaElem.Attribute("Note");
                            decimal alarmParam1 = (decimal)myStSaElem.Attribute("Param1");

                            // Per StalkerActionTemplate:  Add-Alarm Type Stock Value Param1 Note
                            cmd = string.Format("Add-Alarm Type=[{0}] Stock=[{1}] Value=[{2}] Param1=[{3}] Note=[{4}]",
                                                alarmType, stockMeta.STID, alarmValue, alarmParam1, alarmNote);

                            if ((error = stalkerContent.DoAction(cmd)) != StalkerError.OK)
                                throw new Exception("Failed to handle Add-Alarm per PFS.Stocks.Stock.StockAlarms -list");
                        }
                    }
                }

                // Then lets get to Portfolio & StockGroup's 

                foreach (XElement myPfElem in rootPFS.Element("Portfolios").Descendants("Portfolio"))
                {
                    string pfName = (string)myPfElem.Attribute("Name");

                    // Per StalkerActionTemplate: Add-Portfolio PfName
                    cmd = string.Format("Add-Portfolio PfName=[{0}]", pfName);

                    if ((error = stalkerContent.DoAction(cmd)) != StalkerError.OK)
                        throw new Exception("Failed to handle Add-Portfolio PFS.Portfolios.Portfolio -list");

                    // portfolios: Stock Groups

                    foreach (XElement mySgElem in myPfElem.Element("StockGroups").Descendants("StockGroup"))
                    {
                        string sgName = (string)mySgElem.Attribute("Name");

                        // Per StalkerActionTemplate: Add-Group PfName SgName
                        cmd = string.Format("Add-Group PfName=[{0}] SgName=[{1}]", pfName, sgName);

                        if ((error = stalkerContent.DoAction(cmd)) != StalkerError.OK)
                            throw new Exception("Failed to handle Add-Group PFS.Portfolios.Portfolio.StockGroups.StockGroup -list");

                        foreach (XElement mySgStElem in mySgElem.Descendants("Stock")) // Stocks under Stock Group are Follow's on StalkerContent
                        {
                            Guid STID = (Guid)mySgStElem.Attribute("STID");
                            cmd = STID.ToString();

                            if (stalkerContent.StockRef(STID) == null)
                                // Why trying to add stock that is not on main stock list?
                                throw new Exception("Failed to StockGroups containing unknown STID");

                            // Per StalkerActionTemplate: Follow-Group SgName Stock
                            cmd = string.Format("Follow-Group SgName=[{0}] Stock=[{1}]", sgName, STID);

                            if ((error = stalkerContent.DoAction(cmd)) != StalkerError.OK)
                                throw new Exception("Failed to handle Follow-Group PFS.Portfolios.Portfolio.StockGroups.StockGroup -list");
                        }
                    }

                    // portfolios: Stock Orders

                    if (myPfElem.Element("StockOrders") != null)
                    {
                        foreach (XElement myPfOrElem in myPfElem.Element("StockOrders").Descendants())
                        {
                            Guid STID = (Guid)myPfOrElem.Attribute("STID");
                            cmd = STID.ToString();

                            if (stalkerContent.StockRef(STID) == null)
                                throw new Exception("Failed to StockOrder as containing unknown STID");

                            StockOrderType orderType = (StockOrderType)Enum.Parse(typeof(StockOrderType), myPfOrElem.Name.ToString());

                            decimal units = (decimal)myPfOrElem.Attribute("Units");
                            decimal price = (decimal)myPfOrElem.Attribute("Price");
                            DateTime firstDate = (DateTime)myPfOrElem.Attribute("FDate");
                            DateTime lastDate = (DateTime)myPfOrElem.Attribute("LDate");

                            // Per StalkerActionTemplate: Add-Order PfName Type Stock Units Price FirstDate LastDate
                            cmd = string.Format("Add-Order PfName=[{0}] Type=[{1}] Stock=[{2}] Units=[{3}] Price=[{4}] FirstDate=[{5}] LastDate=[{6}]",
                                                pfName, orderType, STID, units, price, firstDate.ToString("yyyy-MM-dd"), lastDate.ToString("yyyy-MM-dd"));

                            if (stalkerContent.DoAction(cmd) != StalkerError.OK)
                                throw new Exception("Failed to handle Add-Order PFS.Portfolios.Portfolio.StockHoldings.StockOrders -list");
                        }
                    }

                    // portfolios: Stock Holdings

                    foreach (XElement myPfShElem in myPfElem.Element("StockHoldings").Descendants("StockHolding"))
                    {
                        Guid STID = (Guid)myPfShElem.Attribute("STID");
                        cmd = STID.ToString();

                        if (stalkerContent.StockRef(STID) == null)
                            // Why trying to add stock that is not on main stock list?
                            throw new Exception("Failed to StockHolding containing unknown STID");

                        decimal pUnits = (decimal)myPfShElem.Attribute("PUnits");
                        decimal price = (decimal)myPfShElem.Attribute("Price");
                        decimal fee = (decimal)myPfShElem.Attribute("Fee");
                        DateTime date = (DateTime)myPfShElem.Attribute("PDate");
                        decimal rUnits = (decimal)myPfShElem.Attribute("RUnits");
                        string holdingID = (string)myPfShElem.Attribute("HoldingID");
                        string holdingNote = (string)myPfShElem.Attribute("Note");
                        decimal conversion = myPfShElem.Attribute("Conversion") == null ? 0 : (decimal)myPfShElem.Attribute("Conversion");
                        CurrencyCode convTo = myPfShElem.Attribute("ConversionTo") == null ? CurrencyCode.USD :
                                                (CurrencyCode)Enum.Parse(typeof(CurrencyCode), (string)myPfShElem.Attribute("ConversionTo"));

                        if ( holdingID == "688190255")
                        {

                        }

                        // Per StalkerActionTemplate: Add-Holding PfName Stock Date Units Price Fee HoldingID Conversion
                        cmd = string.Format("Add-Holding PfName=[{0}] Stock=[{1}] Date=[{2}] Units=[{3}] Price=[{4}]  Fee=[{5}] HoldingID=[{6}] Conversion=[{7}] ConversionTo=[{8}] Note=[{9}]",
                                            pfName, STID, date.ToString("yyyy-MM-dd"), pUnits, price, fee, holdingID, conversion, convTo, holdingNote);

                        if (stalkerContent.DoAction(cmd) != StalkerError.OK)
                            throw new Exception("Failed to handle Add-Holding PFS.Portfolios.Portfolio.StockHoldings.StockHolding -list");

                        // Uses dedicated internal function to import remaining amount properly
                        stalkerContent.HoldingSetRemaining(holdingID, rUnits);

                        // StalkerError 

                        List<StockHolding.DividentsToHolding> dividents = new();

                        foreach (XElement myShDivElem in myPfShElem.Descendants("Divident"))
                        {
                            dividents.Add(new()
                            {
                                Units = (decimal)myShDivElem.Attribute("Units"),
                                DividentID = (string)myShDivElem.Attribute("ID"),
                            });
                        }

                        if ( dividents.Count > 0 )
                        {
                            if ( stalkerContent.HoldingSetDividents(holdingID, dividents) != StalkerError.OK )
                                throw new Exception("Failed to handle Add-Holding divident list");
                        }
                    }

                    // portfolios: Stock Divident
                    if (myPfElem.Element("StockDividents") != null)
                    {
                        foreach (XElement myPfDiElem in myPfElem.Element("StockDividents").Descendants("StockDivident"))
                        {
                            Guid STID = (Guid)myPfDiElem.Attribute("STID");
                            cmd = STID.ToString();

                            if (stalkerContent.StockRef(STID) == null)
                                throw new Exception("Failed to StockDivident containing unknown STID");

                            decimal units = (decimal)myPfDiElem.Attribute("Units");
                            decimal payment = (decimal)myPfDiElem.Attribute("PaymentPerUnit");
                            DateTime date = (DateTime)myPfDiElem.Attribute("Date");
                            string dividentID = (string)myPfDiElem.Attribute("DividentID");
                            decimal conversion = myPfDiElem.Attribute("Conversion") == null ? 0 : (decimal)myPfDiElem.Attribute("Conversion");
                            CurrencyCode convTo = myPfDiElem.Attribute("ConversionTo") == null ? CurrencyCode.USD :
                                                    (CurrencyCode)Enum.Parse(typeof(CurrencyCode), (string)myPfDiElem.Attribute("ConversionTo"));

                            // Using internal function instead of StalkerAPI CMD's as dont wanna re-run holding decisions logic
                            error = stalkerContent.DividentCreate(pfName, STID, date, units, payment, dividentID, conversion, convTo);

                            if (error != StalkerError.OK)
                                throw new Exception("Failed to handle Add-Divident PFS.Portfolios.Portfolio.StockDividents.StockDivident -list");
                        }
                    }

                    // portfolios: Stock Trade

                    foreach (XElement myPfStElem in myPfElem.Element("StockTrades").Descendants("StockTrade"))
                    {
                        Guid STID = (Guid)myPfStElem.Attribute("STID");
                        cmd = STID.ToString();

                        if (stalkerContent.StockRef(STID) == null)
                            // Why trying to add stock that is not on main stock list?
                            throw new Exception("Failed to StockHolding containing unknown STID");

                        decimal sUnits = (decimal)myPfStElem.Attribute("SUnits");
                        decimal pricePerUnit = (decimal)myPfStElem.Attribute("Price");
                        decimal fee = (decimal)myPfStElem.Attribute("Fee");
                        DateTime saleDate = (DateTime)myPfStElem.Attribute("SDate");
                        string tradeID = (string)myPfStElem.Attribute("TradeID");
                        decimal conversion = myPfStElem.Attribute("Conversion") == null ? 0 : (decimal)myPfStElem.Attribute("Conversion");
                        CurrencyCode convTo = myPfStElem.Attribute("ConversionTo") == null ? CurrencyCode.USD :
                                                (CurrencyCode)Enum.Parse(typeof(CurrencyCode), (string)myPfStElem.Attribute("ConversionTo"));
                        List<StockTrade.SoldHoldingType> holdings = new();

                        foreach (XElement myTradeHoldingElem in myPfStElem.Descendants("SoldHolding"))
                        {
                            holdings.Add(new()
                            {
                                HoldingID = (string)myTradeHoldingElem.Attribute("HoldingID"),
                                SoldUnits = (decimal)myTradeHoldingElem.Attribute("SoldUnits"),
                            });
                        }

                        StalkerError err = stalkerContent.TradeCreate(pfName, STID, saleDate, sUnits, pricePerUnit, fee, tradeID, conversion, convTo, holdings);

                        if (err != StalkerError.OK)
                            throw new Exception("Failed to handle TradeCreate");
                    }
                }

                return StalkerError.OK;
            }
            catch (Exception e)
            {
                Log.Warning(e, "StalkerXML:ImportXml() - Exception, with cmd: " + cmd);
            }
            return error;
        }
    }
}
