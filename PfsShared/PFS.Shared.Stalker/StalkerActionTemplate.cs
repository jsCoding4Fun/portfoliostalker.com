/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Collections.Immutable;
using System.Linq;

namespace PFS.Shared.Stalker
{
    // Provides template for each Stalker Action combo of its expected parameters, and their allowed ranges/formattings
    internal class StalkerActionTemplate
    {
        // string per expected parameter, on order, presenting Name of field and its expected content
        public static string[] Get(StalkerOperation Operation, StalkerElement Element)
        {
            ActionTemplate template = Templates.Where(t => t.Operation == Operation && t.Element == Element).SingleOrDefault();

            if (template == null)
                return null;

            return template.Params.Split(' ');
        }

        protected class ActionTemplate
        {
            public StalkerOperation Operation { get; set; }
            public StalkerElement Element { get; set; }
            public string Params { get; set; }
        };

        protected readonly static ImmutableArray<ActionTemplate> Templates = ImmutableArray.Create(new ActionTemplate[]
        {
#region PORTFOLIO
            
            new ActionTemplate()                            // Add-Portfolio PfName
            {
                Operation = StalkerOperation.Add,
                Element = StalkerElement.Portfolio,
                Params = "PfName=String:1:20:CharSetPfName",
            },

            new ActionTemplate()                            // Edit-Portfolio PfCurrName PfNewName
            {
                Operation = StalkerOperation.Edit,
                Element = StalkerElement.Portfolio,
                Params = "PfCurrName=String:1:20:CharSetPfName PfNewName=String:1:20:CharSetPfName",
            },

            new ActionTemplate()                            // Delete-Portfolio PfName
            {
                Operation = StalkerOperation.Delete,
                Element = StalkerElement.Portfolio,
                Params = "PfName=String:1:20:CharSetPfName",
            },

            new ActionTemplate()                            // Top-Portfolio PfName
            {
                Operation = StalkerOperation.Top,
                Element = StalkerElement.Portfolio,
                Params = "PfName=String:1:20:CharSetPfName",
            },

#endregion

#region STOCK GROUP

            new ActionTemplate()                            // Add-Group PfName SgName
            {
                Operation = StalkerOperation.Add,
                Element = StalkerElement.Group,
                Params = "PfName=String:1:20:CharSetPfName SgName=String:1:20:CharSetSgName",
            },

            new ActionTemplate()                            // Edit-Group SgCurrName SgNewName
            {
                Operation = StalkerOperation.Edit,
                Element = StalkerElement.Group,
                Params = "SgCurrName=String:1:20:CharSetSgName SgNewName=String:1:20:CharSetSgName",
            },

            new ActionTemplate()                            // Delete-Group SgName
            {
                Operation = StalkerOperation.Delete,
                Element = StalkerElement.Group,
                Params = "SgName=String:1:20:CharSetSgName",
            },

            new ActionTemplate()                            // Top-Group SgName
            {
                Operation = StalkerOperation.Top,
                Element = StalkerElement.Group,
                Params = "SgName=String:1:20:CharSetSgName",
            },

            new ActionTemplate()                            // Follow-Group SgName Stock
            {
                Operation = StalkerOperation.Follow,
                Element = StalkerElement.Group,
                Params = "SgName=String:1:20:CharSetSgName Stock=STID",
            },

            new ActionTemplate()                            // Unfollow-Group SgName Stock
            {
                Operation = StalkerOperation.Unfollow,
                Element = StalkerElement.Group,
                Params = "SgName=String:1:20:CharSetSgName Stock=STID",
            },

#endregion

#region HOLDING

            new ActionTemplate()                            // Add-Holding PfName Stock Date Units Price Fee HoldingID Conversion ConversionTo Note
            {
                Operation = StalkerOperation.Add,
                Element = StalkerElement.Holding,
                Params = "PfName=String:1:20:CharSetPfName Stock=STID Date=Date Units=Decimal:0.01 Price=Decimal:0.01 "+
                         "Fee=Decimal:0.00 HoldingID=HoldingID Conversion=Decimal:0.0 ConversionTo=CurrencyCode " +
                         "Note=String:0:100:CharSetHoldingNote",
            },

            new ActionTemplate()                            // Edit-Holding HoldingID Date Units Price Fee Conversion ConversionTo Note
            {
                Operation = StalkerOperation.Edit,
                Element = StalkerElement.Holding,
                Params = "HoldingID=HoldingID Date=Date Units=Decimal:0.01 Price=Decimal:0.01 Fee=Decimal:0.00 " +
                         "Conversion=Decimal:0.0 ConversionTo=CurrencyCode Note=String:0:100:CharSetHoldingNote",
            },

            new ActionTemplate()                            // Delete-Holding HoldingID
            {
                Operation = StalkerOperation.Delete,
                Element = StalkerElement.Holding,
                Params = "HoldingID=HoldingID",
            },

#endregion

#region TRADE / SALE

            new ActionTemplate()                            // Add-Trade PfName Stock Date Units Price Fee TradeID HoldingStrID Conversion ConversionTo
            {
                Operation = StalkerOperation.Add,
                Element = StalkerElement.Trade,
                Params = "PfName=String:1:20:CharSetPfName Stock=STID Date=Date Units=Decimal:0.01 Price=Decimal:0.01 "+
                         "Fee=Decimal:0.00 TradeID=TradeID HoldingStrID=String Conversion=Decimal:0.0 ConversionTo=CurrencyCode",
            },

            new ActionTemplate()                            // Delete-Trade TradeID
            {
                Operation = StalkerOperation.Delete,
                Element = StalkerElement.Trade,
                Params = "TradeID=TradeID",
            },

#endregion

#region DIVIDENT

            new ActionTemplate()                            // Add-Divident PfName Stock Date Units PaymentPerUnit DividentID Conversion ConversionTo
            {
                Operation = StalkerOperation.Add,
                Element = StalkerElement.Divident,
                Params = "PfName=String:1:20:CharSetPfName Stock=STID Date=Date Units=Decimal:0.01 PaymentPerUnit=Decimal:0.01 "+
                         "DividentID=DividentID Conversion=Decimal:0.0 ConversionTo=CurrencyCode",
            },

            new ActionTemplate()                            // Delete-Divident DividentID
            {
                Operation = StalkerOperation.Delete,
                Element = StalkerElement.Divident,
                Params = "DividentID=DividentID",
            },

#endregion

#region ORDER

            new ActionTemplate()                            // Add-Order PfName Type Stock Units Price FirstDate LastDate
            {
                Operation = StalkerOperation.Add,
                Element = StalkerElement.Order,
                Params = "PfName=String:1:20:CharSetPfName Type=StockOrderType Stock=STID Units=Decimal:0.01 Price=Decimal:0.01 FirstDate=Date LastDate=Date",
            },

            new ActionTemplate()                            // Edit-Order PfName Type Stock EditedPrice Units Price FirstDate LastDate
            {
                Operation = StalkerOperation.Edit,
                Element = StalkerElement.Order,
                Params = "PfName=String:1:20:CharSetPfName Type=StockOrderType Stock=STID EditedPrice=Decimal:0.01 Units=Decimal:0.01 Price=Decimal:0.01 FirstDate=Date LastDate=Date",
            },

            new ActionTemplate()                            // Delete-Order PfName Stock Price
            {
                Operation = StalkerOperation.Delete,
                Element = StalkerElement.Order,
                Params = "PfName=String:1:20:CharSetPfName Stock=STID Price=Decimal:0.01",
            },

#endregion

#region ALARM

            new ActionTemplate()                            // Add-Alarm Type Stock Value Param1 Note
            {
                Operation = StalkerOperation.Add,
                Element = StalkerElement.Alarm,
                Params = "Type=StockAlarmType Stock=STID Value=Decimal:0.01 Param1=Decimal:0.01 Note=String:0:100",
            },

            new ActionTemplate()                            // Delete-Alarm Stock Value
            {
                Operation = StalkerOperation.Delete,
                Element = StalkerElement.Alarm,
                Params = "Stock=STID Value=Decimal:0.01",
            },

            new ActionTemplate()                            // DeleteAll-Alarm Stock
            {
                Operation = StalkerOperation.DeleteAll,
                Element = StalkerElement.Alarm,
                Params = "Stock=STID",
            },

            new ActionTemplate()                            // Edit-Alarm Type Stock EditedValue Value Param1 Note
            {
                Operation = StalkerOperation.Edit,
                Element = StalkerElement.Alarm,
                Params = "Type=StockAlarmType Stock=STID EditedValue=Decimal:0.01 Value=Decimal:0.01 Param1=Decimal:0.01 Note=String:0:100",
            },

#endregion

#region STOCK

            new ActionTemplate()                            // Add-Stock MarketID Ticker CompanyName Stock
            {
                Operation = StalkerOperation.Add,
                Element = StalkerElement.Stock,
                Params = "MarketID=MarketID Ticker=String:1:20:CharSetTicker CompanyName=String:1:100:CharSetCompanyName Stock=STID",
            },

            new ActionTemplate()                            // Edit-Stock Ticker CompanyName Stock
            {
                Operation = StalkerOperation.Edit,
                Element = StalkerElement.Stock,
                Params = "Ticker=String:1:20:CharSetTicker CompanyName=String:1:100:CharSetCompanyName Stock=STID",
            },

            new ActionTemplate()                            // Delete-Stock Stock
            {
                Operation = StalkerOperation.Delete,
                Element = StalkerElement.Stock,
                Params = "Stock=STID",
            },

            new ActionTemplate()                            // Set-Stock Stock [+LastEdit] 
            {
                Operation = StalkerOperation.Set,
                Element = StalkerElement.Stock,
                Params = "Stock=STID +LastEdit=Date",
            },

#endregion

        });
    }
}
