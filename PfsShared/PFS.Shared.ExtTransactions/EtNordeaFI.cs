/*
 * Copyright (c) 2021 Jami Suni
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

using PFS.Shared.Types;

namespace PFS.Shared.ExtTransactions
{
    // Convert http://nordea.fi/ CSV exports to PFS specific broker transaction processing format
    public class EtNordeaFI : IExtTransactions      // !!!REMEMBER!!! When saving as CSV from Excel w SaveAs DO IT AS: CSV / Utf8
    {
        // Note! Sure does return Divident information as ""Osinko"" but that record is absolute garbage w no information to use anything -> POSTPONED far far away


        public string Convert2RawString(byte[] byteContent)
        {
            return System.Text.Encoding.UTF8.GetString(byteContent);
        }

        public List<ExtTransaction> Convert2ExtTransaction(byte[] byteContent)
        {
            List<ExtTransaction> conversionResult = null;

            var config = new CsvConfiguration(CultureInfo.InvariantCulture) // dont seam to make any effect, so stick to default... CultureInfo.GetCultureInfo("fi-FI")
            {
                Delimiter = ",",
            };

            string fullContent = Convert2RawString(byteContent);

            fullContent = fullContent.Replace("   ,", ","); // This should get rid of those spaces on end of everything before delimiter
            fullContent = fullContent.Replace("  ,", ",");
            fullContent = fullContent.Replace(" ,", ",");

            using (var csv = new CsvReader(new StringReader(fullContent), config))
            {
                csv.Context.RegisterClassMap<NordeaFiCsvFormatMap>();
                conversionResult = csv.GetRecords<ExtTransaction>().ToList();
            }

            List<ExtTransaction> ret = new();

            foreach (ExtTransaction entry in conversionResult)
            {
                if (entry.ConversionRate != 1 && entry.Fee > 0)
                    entry.Fee = entry.Fee / entry.ConversionRate; // Carefull! Assumes Fee given as Euros, so fixing it to purhace currency

                if (entry.PaymentDate == DateTime.MinValue)
                    entry.PaymentDate = entry.RecordDate;

                if (entry.RecordDate == DateTime.MinValue)
                    entry.RecordDate = entry.PaymentDate;

                // Nordea doesnt have uniqueID but has 'Toimeksiantonumero' that is per one purhace order, but maybe performed as multiple ones
                // so this case we need to keep merging multiple entry's to one w merge calculations... and yes may have other events between them

                ExtTransaction samePurhace = ret.SingleOrDefault(e => e.UniqueID == entry.UniqueID);

                if (samePurhace != null)
                {
                    if (entry.Units > 0)
                    {
                        samePurhace.AmountPerUnit = (samePurhace.AmountPerUnit * samePurhace.Units + entry.AmountPerUnit * entry.Units)
                                                  / (samePurhace.Units + entry.Units);

                        samePurhace.ConversionRate = (samePurhace.ConversionRate * samePurhace.Units + entry.ConversionRate * entry.Units)
                                                   / (samePurhace.Units + entry.Units);

                        samePurhace.Units += entry.Units;
                    }

                    samePurhace.Fee += entry.Fee;

                    // Note! Throwing out a additional record, as all hopefully recorded to merge record
                }
                else
                    // New event, so add to return list
                    ret.Add(entry);
            }

            // Fix empty 'PaymentDate' & merge entrys w same 'UniqueID' 

            return ret;
        }

        protected sealed class NordeaFiCsvFormatMap : ClassMap<ExtTransaction>
        {
            public NordeaFiCsvFormatMap()
            {
                Map(m => m.UniqueID).Name("Toimeksiantonumero");    // Note! This is NOT Unique as may have multiple buys under one.. so need to merge those!

                Map(m => m.Company.Market).Name("Pörssi").TypeConverter<NordeaFi2MarketTypeConverter>();
                Map(m => m.Company.Ticker).Name("Kaupankäyntitunnus");
                Map(m => m.Company.Name).Name("Nimi");
                Map(m => m.Company.ISIN).Name("ISIN koodi");
                Map(m => m.Type).Name("Tapahtumatyyppi").TypeConverter<NordeaFi2EtTypeConverter>();
                Map(m => m.RecordDate).Name("Kauppapäivä").TypeConverter<NordeaFi2DateTypeConverter>();
                Map(m => m.PaymentDate).Name("Maksupäivä").TypeConverter<NordeaFi2DateTypeConverter>();
                Map(m => m.Units).Name("Määrä").TypeConverter<NordeaFi2DecimalTypeConverter>();
                Map(m => m.AmountPerUnit).Name("Kurssi");
                Map(m => m.Currency).Index(10).TypeConverter<NordeaFi2CurrencyCodeTypeConverter>();         // Carefull! Has to refer w index as duplicate names!
                Map(m => m.ConversionRate).Name("Valuuttakurssi");
                                                        // Note! FEE! Atm assumes EUR always, so needs conversion as PFS except on original currency!
                Map(m => m.Fee).Name("Palkkio").TypeConverter<NordeaFi2DecimalTypeConverter>(); // Custom converter as leaving field empty on dividents
                Map(m => m.Note).Ignore();

#if false // As of Nordea 2021-Aug-28th format

Toimeksiantonumero, Tapahtumatyyppi,    Nimi                   ,ISIN koodi     ,Kaupankäyntitunnus     ,Pörssi,
000024116357,       Osto,               TOREX GOLD RESOURCES   ,CA8910546032   ,TXG                    ,Toronto   ,

Kauppapäivä,        Maksupäivä,         Määrä,                  Kurssi,         Valuutta,               Valuuttakurssi,
23.07.2021,         ,                   70,                     13.18,          CAD   ,                 0.6776,

Market value(Traded)(label.marketvaluelocal),   Valuutta,   Market value(Settled)(label.marketvalueforeign),    Valuutta,   Palkkio,    Valuutta,
922.58559622196,                                CAD   ,     625.144,                                            EUR   ,     2.07,       EUR   

Muut kulut,     Valuutta,       Transaction amount(Traded)(label.transactiontotallocal),    Valuutta,
,0,             CAD   ,         925.640495867769,                                           CAD   ,

Transaction amount(Settled)(label.transactiontotalforeign),     Valuutta,   Rahatili,       Tili,                   Realisoitunut tuotto,Valuutta
627.214,                                                        EUR         ,,              02 2000 13955943 3      ,                       ,



000024116357   ,Osto   ,TOREX GOLD RESOURCES   ,CA8910546032   ,TXG   ,Toronto   ,23.07.2021,,100,13.18,CAD   ,0.6779,1317.9319958696,CAD   ,893.4261,EUR   ,2.96,EUR   ,0,CAD   ,1322.29842159611,CAD   ,896.3861,EUR   ,,02 2000 13955943 3   ,,
000024116357   ,Osto   ,TOREX GOLD RESOURCES   ,CA8910546032   ,TXG   ,Toronto   ,23.07.2021,,100,13.18,CAD   ,0.6779,1317.90514825195,CAD   ,893.4079,EUR   ,2.96,EUR   ,0,CAD   ,1322.27157397846,CAD   ,896.3679,EUR   ,,02 2000 13955943 3   ,,



Divident from: "".....,Osinko,METSO,000000012575,,null,-,Tue Nov 05 00:00:00 CET 2019,,0,,0,,,30,EUR,0,EUR,,,,,30,EUR,,02 2000 10394385 2,,""
=> Absolute useless.. so has day, name, and total amount.. leaves a way too much detective work to do.. so not worth of effort

Toimeksiantonumero, Tapahtumatyyppi,    Nimi,   ISIN koodi,     Kaupankäyntitunnus, Pörssi,     Kauppapäivä     ,Maksupäivä,
.....   ,           Osinko   ,          METSO   ,000000012575   ,                 , null   ,    -,               Tue Nov 05 00:00:00 CET 2019   ,

Määrä,  Kurssi, Valuutta,   Valuuttakurssi,     Market value(Traded)(label.marketvaluelocal),   Valuutta,
        ,0      ,,          0,                  ,                                               ,           ,

Market value(Settled)(label.marketvalueforeign),    Valuutta,   Palkkio,    Valuutta,   
30,                                                 EUR   ,     0,          EUR   ,

Muut kulut,     Valuutta,   Transaction amount(Traded)(label.transactiontotallocal),    Valuutta,
,               ,           ,                                                           ,

Transaction amount(Settled)(label.transactiontotalforeign),     Valuutta,   Rahatili,   Tili,               Realisoitunut tuotto,   Valuutta
30,                                                             EUR,        ,           02 2000 10394385 2, ,

#endif
            }
        }

        public class NordeaFi2EtTypeConverter : TypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                switch (text)
                {
                    case "Osto": return ExtTransaction.EtType.Buy;
                    case "Myynti": return ExtTransaction.EtType.Sell;
                    //case "Osinko": return EtRecord.EtType.Divident; Useless information, only gives day + total payment.. no units.. no per unit.. lazy asses..
                }
                return ExtTransaction.EtType.Unknown;
            }
        }

        public class NordeaFi2MarketTypeConverter : TypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                switch (text)
                {
                    case "Toronto": return "TSX";
                }
                return "";
            }
        }

        // 

        public class NordeaFi2CurrencyCodeTypeConverter : TypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                switch (text)
                {
                    case "USD": return CurrencyCode.USD;
                    case "CAD": return CurrencyCode.CAD;
                    case "EUR": return CurrencyCode.EUR;
                }
                return CurrencyCode.Unknown;
            }
        }

        public class NordeaFi2DecimalTypeConverter : TypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (string.IsNullOrWhiteSpace(text) == true)
                    return new Decimal(0.0);

                return Decimal.Parse(text);
            }
        }

        public class NordeaFi2DateTypeConverter : TypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                if (string.IsNullOrWhiteSpace(text) == true)
                    return new DateTime();

                if ( text == "-")
                    return new DateTime();

                if ( text.Length == 10 )
                    return DateTime.ParseExact(text, "dd.MM.yyyy", CultureInfo.InvariantCulture); // 23.07.2021

                if ( text.Length == "Tue Nov 05 00:00:00 CET 2019".Length || text.Length == "Fri Oct 11 00:00:00 CEST 2019".Length)
                {
                    string[] split = text.Split(' ');

                    string merge = split[2] + "." + split[1] + "." + split[5];

                    return DateTime.ParseExact(merge, "dd.MMM.yyyy", CultureInfo.InvariantCulture);
                }

                return DateTime.Parse(text);
            }
        }

        public class RowNumberConverter : DefaultTypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                return row.Context.Parser.Row;
            }
        }
    }
}
