/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using PFS.Shared.Types;
using PFS.Shared.Stalker;

namespace PFS.Shared.StalkerAddons
{
    // Provides generic Validate & Convert a external Brokers 'ExtTransaction' records to Stalker Action's format
    public class StalkerExtTransactions
    {
        protected string _portfolioName;
        protected CurrencyCode _homeCurrency = CurrencyCode.Unknown;
        protected StalkerContent _stalkerContent = null;

        public StalkerExtTransactions(string portfolioName, CurrencyCode homeCurrency, StalkerContent stalkerContent)
        {
            _portfolioName = portfolioName;
            _homeCurrency = homeCurrency;
            _stalkerContent = stalkerContent;
        }

        // Does check & complete transactions information to make sure everything required is available
        public bool Validate(ExtTransaction etTrans)
        {
            etTrans.Status = ExtTransaction.ProcessingStatus.Acceptable;

            switch ( etTrans.Type )
            {
                case ExtTransaction.EtType.Buy:
                case ExtTransaction.EtType.Sell:
                case ExtTransaction.EtType.Divident:
                {
                        if (etTrans.Units <= 0)
                            etTrans.Status = ExtTransaction.ProcessingStatus.Error_UnitAmount;
                        
                        else if (etTrans.AmountPerUnit <= 0)
                            etTrans.Status = ExtTransaction.ProcessingStatus.Error_PricePerUnit;

                        else if (etTrans.Fee < 0)
                            etTrans.Status = ExtTransaction.ProcessingStatus.Error_Fee;
                    }
                    break;

                default:
                    etTrans.Status = ExtTransaction.ProcessingStatus.Error_UnknownRecordType;
                    break;
            }

            if (etTrans.Status != ExtTransaction.ProcessingStatus.Acceptable)
                return false;

            switch (etTrans.Type)
            {
                case ExtTransaction.EtType.Buy:
                    {
                        if (_stalkerContent.StockHoldingRef(etTrans.UniqueID) != null )
                            etTrans.Status = ExtTransaction.ProcessingStatus.Duplicate;
                    }
                    break;

                case ExtTransaction.EtType.Sell:
                    {
                        if (_stalkerContent.StockTradeRef(etTrans.UniqueID) != null)
                            etTrans.Status = ExtTransaction.ProcessingStatus.Duplicate;
                    }
                    break;

                case ExtTransaction.EtType.Divident:
                    {
                        if (_stalkerContent.StockDividentRef(etTrans.UniqueID) != null)
                            etTrans.Status = ExtTransaction.ProcessingStatus.Duplicate;
                    }
                    break;
            }

            if (etTrans.Status != ExtTransaction.ProcessingStatus.Acceptable)
                return false;

            return true;
        }

        // Assumes everything is Validated, so this creates actual Stalker compatible Action commands, return empty for error
        public string Convert(ExtTransaction etTrans)
        {
            if (string.IsNullOrWhiteSpace(_portfolioName) == true)
                return string.Empty;

            switch (etTrans.Type)
            {
                case ExtTransaction.EtType.Buy:

                    return string.Format("Add-Holding PfName=[{0}] Stock=[{1}] Date=[{2}] Units=[{3}] Price=[{4}] "
                                        + "Fee=[{5}] HoldingID=[{6}] Conversion=[{7}] ConversionTo=[{8}] Note=[{9}]",
                                         _portfolioName, etTrans.Company.STID.ToString(), etTrans.RecordDate.ToString("yyyy-MM-dd"), etTrans.Units, etTrans.AmountPerUnit,
                                         etTrans.Fee, etTrans.UniqueID, etTrans.ConversionRate, _homeCurrency.ToString(), etTrans.Note);

                case ExtTransaction.EtType.Sell:

                    return string.Format("Add-Trade PfName=[{0}] Stock=[{1}] Date=[{2}] Units=[{3}] Price=[{4}] "
                                        + "Fee=[{5}] TradeID=[{6}] HoldingStrID=[] Conversion=[{7}] ConversionTo=[{8}]",
                                               _portfolioName, etTrans.Company.STID.ToString(), etTrans.RecordDate.ToString("yyyy-MM-dd"), etTrans.Units, etTrans.AmountPerUnit,
                                               etTrans.Fee, etTrans.UniqueID, etTrans.ConversionRate, _homeCurrency.ToString(), etTrans.Note);

                case ExtTransaction.EtType.Divident:

                    return string.Format("Add-Divident PfName=[{0}] Stock=[{1}] Date=[{2}] Units=[{3}] PaymentPerUnit=[{4}] "
                                        +"DividentID=[{5}] Conversion=[{6}] ConversionTo=[{7}]",
                                               _portfolioName, etTrans.Company.STID.ToString(), etTrans.PaymentDate.ToString("yyyy-MM-dd"), etTrans.Units, etTrans.AmountPerUnit,
                                               etTrans.UniqueID, etTrans.ConversionRate, _homeCurrency.ToString());
            }
            return string.Empty;
        }
    }
}
