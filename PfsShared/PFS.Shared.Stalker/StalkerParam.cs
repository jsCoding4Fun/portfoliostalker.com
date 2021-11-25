/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Globalization;
using System.Linq;

using PFS.Shared.Types;

namespace PFS.Shared.Stalker
{
    // Handles single parameter for StalkerAction, validating and parsing it out
    public class StalkerParam
    {
        public string Name { get; internal set; } = string.Empty;

        protected string Value { get; set; } = string.Empty;

        protected string Template { get; set; } = string.Empty;

        public ParamType Type { get; internal set; } = ParamType.Unknown;

        public StalkerError Error { get; internal set; } = StalkerError.Unknown; // This should be OK only if acceptable 'Value' is set

        public StalkerParam(string template) // See StalkerActionTemplate for template
        {
            string[] split = template.Split('=');

            if (split.Count() != 2)
                return;
            
            Name = split[0];
            Template = split[1];

            if (Name.StartsWith('+') == true)
                // indicates optional parameter, that is OK by default even not set.. so either empty or needs to be set
                Error = StalkerError.OK;

            string[] templateSegments = Template.Split(':');
            Type = (ParamType)Enum.Parse(typeof(ParamType), templateSegments[0]);
        }

        public StalkerError Parse(string param)
        {
            /* Double:0.01:100.00      MIN,MAX          => double on defined range if min/max set.. barely used atm as now all Decimals
             * STID                                     => Guid so expecting a STID
             * String:0:100:CharSetNote                 => string with min/max length, possible char set checking against named char set template
             * Date                                     => "yyyy-MM-dd"
             * StockAlarmType                           => Per enum values
             * StockOrderType                           => Per enum values
             * HoldingID                                => special string w support for 'empty' that changes it to use random uniqueID, or can be user defined
             * MarketID
             * TradeID                                  => unique value for Sales ala Trades
             * Decimal:0.01:100.00                      => for currency values
             * CurrencyCode                             => USD etc per enum values
             * DividentID                               => unique value for Dividents
             * 
             * Number
             * Procent
             * 
             */

            if (param.Contains('=') == true)
            {
                if (LocalCheckName(param) == false)
                    // If param contains '=' then name must be match
                    return (Error = StalkerError.InvalidParameter);

                // Rest of code doesnt care name...
                param = param.Split('=')[1];
            }

            string[] templateSegments = Template.Split(':');

            switch ( Type )
            {
                case ParamType.Double:
                    {
                        double value = LocalGetDouble(templateSegments, param);

                        if (Error != StalkerError.OK)
                            return Error;

                        Value = value.ToString();
                        return (Error = StalkerError.OK);
                    }

                case ParamType.Decimal:
                    {
                        decimal value = LocalGetDecimal(templateSegments, param);

                        if (Error != StalkerError.OK)
                            return Error;

                        Value = value.ToString();
                        return (Error = StalkerError.OK);
                    }

                case ParamType.STID:
                    {
                        Guid STID;
                        if ( Guid.TryParse(param, out STID) == false )
                            // not proper Guid
                            return (Error = StalkerError.InvalidParameter);

                        Value = STID.ToString();
                        return (Error = StalkerError.OK);
                    }

                case ParamType.String:
                    {
                        string value = LocalGetString(templateSegments, param);

                        if (Error != StalkerError.OK)
                            return Error;

                        Value = value;
                        return (Error = StalkerError.OK);
                    }

                case ParamType.Date:
                    {
                        DateTime? date = LocalGetDate(param);

                        if (date.HasValue == false)
                            return (Error = StalkerError.InvalidParameter);

                        Value = date.Value.ToString("yyyy-MM-dd");
                        return (Error = StalkerError.OK);
                    }

                case ParamType.StockAlarmType:
                    {
                        StockAlarmType alarmType = (StockAlarmType)Enum.Parse(typeof(StockAlarmType), param);

                        if (alarmType == StockAlarmType.Unknown)
                            return (Error = StalkerError.Unknown);

                        Value = alarmType.ToString();
                        return (Error = StalkerError.OK);
                    }

                case ParamType.CurrencyCode:
                    {
                        CurrencyCode currency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), param);

                        if (currency == CurrencyCode.Unknown)
                            return (Error = StalkerError.Unknown);

                        Value = currency.ToString();
                        return (Error = StalkerError.OK);
                    }

                case ParamType.MarketID:
                    {
                        MarketID marketID = (MarketID)Enum.Parse(typeof(MarketID), param);

                        if (marketID == MarketID.Unknown)
                            return (Error = StalkerError.Unknown);

                        Value = marketID.ToString();
                        return (Error = StalkerError.OK);
                    }

                case ParamType.StockOrderType:
                    {
                        StockOrderType orderType = (StockOrderType)Enum.Parse(typeof(StockOrderType), param);

                        if (orderType == StockOrderType.Unknown)
                            return (Error = StalkerError.Unknown);

                        Value = orderType.ToString();
                        return (Error = StalkerError.OK);
                    }

                case ParamType.HoldingID:
                    {
                        string value = LocalGetHoldingID(param);

                        if (Error != StalkerError.OK)
                            return Error;

                        Value = value;
                        return (Error = StalkerError.OK);
                    }

                case ParamType.TradeID:
                    {
                        string value = LocalGetTradeID(param);

                        if (Error != StalkerError.OK)
                            return Error;

                        Value = value;
                        return (Error = StalkerError.OK);
                    }

                case ParamType.DividentID:
                    {
                        string value = LocalGetDividentID(param);

                        if (Error != StalkerError.OK)
                            return Error;

                        Value = value;
                        return (Error = StalkerError.OK);
                    }
            }
            return (Error = StalkerError.InvalidParameter);

            // LOCAL FUNCTIONS, dang I start to love these :D maybe even stop passing in those 'known variables' 

            bool LocalCheckName(string param)
            {
                string[] split = param.Split('=');

                if (split.Count() != 2)
                    return false;

                if (split[0] != Name)
                    return false;

                return true;
            }

            double LocalGetDouble(string[] templateSegments, string param)
            {
                double value;

                if (double.TryParse(param, out value) == false)
                {
                    Error = StalkerError.InvalidParameter;
                    return 0;
                }

                if ( templateSegments.Count() >= 2 && templateSegments[1].Length > 0)
                {
                    double min = double.Parse(templateSegments[1]);

                    if (value < min)
                    {
                        Error = StalkerError.OutOfRangeParameter;
                        return 0;
                    }
                }

                if (templateSegments.Count() >= 3 && templateSegments[2].Length > 0)
                {
                    double max = double.Parse(templateSegments[2]);

                    if (value > max)
                    {
                        Error = StalkerError.OutOfRangeParameter;
                        return 0;
                    }
                }

                Error = StalkerError.OK;
                return value;
            }

            decimal LocalGetDecimal(string[] templateSegments, string param)
            {
                decimal value;

                if (decimal.TryParse(param, out value) == false)
                {
                    Error = StalkerError.InvalidParameter;
                    return 0;
                }

                if (templateSegments.Count() >= 2 && templateSegments[1].Length > 0)
                {
                    decimal min = decimal.Parse(templateSegments[1]);

                    if (value < min)
                    {
                        Error = StalkerError.OutOfRangeParameter;
                        return 0;
                    }
                }

                if (templateSegments.Count() >= 3 && templateSegments[2].Length > 0)
                {
                    decimal max = decimal.Parse(templateSegments[2]);

                    if (value > max)
                    {
                        Error = StalkerError.OutOfRangeParameter;
                        return 0;
                    }
                }

                Error = StalkerError.OK;
                return value;
            }

            string LocalGetString(string[] templateSegments, string param)
            {
                if (templateSegments.Count() >= 2 && templateSegments[1].Length > 0)
                {
                    int minLength = int.Parse(templateSegments[1]);

                    if ( minLength > param.Length )
                    {
                        Error = StalkerError.OutOfRangeParameter;
                        return string.Empty;
                    }
                }

                if (templateSegments.Count() >= 3 && templateSegments[2].Length > 0)
                {
                    int maxLength = int.Parse(templateSegments[2]);

                    if (maxLength < param.Length)
                    {
                        Error = StalkerError.OutOfRangeParameter;
                        return string.Empty;
                    }
                }

                if (templateSegments.Count() >= 4 && templateSegments[3].Length > 0)
                {
                    StalkerValidate.CharSet charSet = (StalkerValidate.CharSet)Enum.Parse(typeof(StalkerValidate.CharSet), templateSegments[3]);

                    if (StalkerValidate.String(param, charSet) == false)
                    {
                        Error = StalkerError.InvalidCharSet;
                        return string.Empty;
                    }
                }

                Error = StalkerError.OK;
                return param;
            }

            string LocalGetHoldingID(string param)
            {
                if ( param.Length > 50 )
                {
                    Error = StalkerError.InvalidParameter;
                    return string.Empty;
                }

                if ( param.Length == 0 )
                {
                    // Special case, if client didnt define HoldingID then we assign a unique ID for it
                    Error = StalkerError.OK;
                    return "PFSHLDID: " + Guid.NewGuid().ToString();
                }

                if (StalkerValidate.String(param, StalkerValidate.CharSet.CharSetHoldingID) == false)
                {
                    Error = StalkerError.InvalidCharSet;
                    return string.Empty;
                }

                Error = StalkerError.OK;
                return param;
            }

            string LocalGetTradeID(string param)
            {
                if (param.Length > 50)
                {
                    Error = StalkerError.InvalidParameter;
                    return string.Empty;
                }

                if (param.Length == 0)
                {
                    // Special case, if client didnt define TradeID then we assign a unique ID for it
                    Error = StalkerError.OK;
                    return "PFSTRDID: " + Guid.NewGuid().ToString();
                }

                if (StalkerValidate.String(param, StalkerValidate.CharSet.CharSetTradeID) == false)
                {
                    Error = StalkerError.InvalidCharSet;
                    return string.Empty;
                }

                Error = StalkerError.OK;
                return param;
            }

            string LocalGetDividentID(string param)
            {
                if (param.Length > 50)
                {
                    Error = StalkerError.InvalidParameter;
                    return string.Empty;
                }

                if (param.Length == 0)
                {
                    // Special case, if client didnt define then we assign a unique ID for it
                    Error = StalkerError.OK;
                    return "PFSDIVID: " + Guid.NewGuid().ToString();
                }

                if (StalkerValidate.String(param, StalkerValidate.CharSet.CharSetDividentID) == false)
                {
                    Error = StalkerError.InvalidCharSet;
                    return string.Empty;
                }

                Error = StalkerError.OK;
                return param;
            }

            DateTime? LocalGetDate(string param)
            {
                DateTime result;

                if (DateTime.TryParseExact(param, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out result) == false)
                    return null;

                return result;
            }
        }

        public static implicit operator string(StalkerParam param) { return param.Value; }
        public static implicit operator Guid(StalkerParam param) { return Guid.Parse(param.Value); }
        public static implicit operator double(StalkerParam param) { return double.Parse(param.Value); }
        public static implicit operator decimal(StalkerParam param) { return decimal.Parse(param.Value); }
        public static implicit operator DateTime(StalkerParam param) { return DateTime.ParseExact(param.Value, "yyyy-MM-dd", CultureInfo.InvariantCulture); }
        public static implicit operator StockAlarmType(StalkerParam param) {  return (StockAlarmType)Enum.Parse(typeof(StockAlarmType), param.Value); }
        public static implicit operator StockOrderType(StalkerParam param) { return (StockOrderType)Enum.Parse(typeof(StockOrderType), param.Value); }
        public static implicit operator MarketID(StalkerParam param) { return (MarketID)Enum.Parse(typeof(MarketID), param.Value); }
        public static implicit operator CurrencyCode(StalkerParam param) { return (CurrencyCode)Enum.Parse(typeof(CurrencyCode), param.Value); }

        public enum ParamType : int
        {
            Unknown = 0,
            Double,
            Decimal,        // All currency related values
            String,
            Date,
            STID,
            StockAlarmType,
            StockOrderType,
            HoldingID,
            TradeID,
            DividentID,
            CurrencyCode,
            MarketID,
        }
    }
}
