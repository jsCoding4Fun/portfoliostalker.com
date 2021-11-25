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
    // Convert http://nordnet.fi/ CSV exports to PFS specific broker transaction processing format
    public class EtNordnetFI : IExtTransactions
    {
        public string Convert2RawString(byte[] byteContent)
        {
            return System.Text.Encoding.Unicode.GetString(byteContent);
        }

        public List<ExtTransaction> Convert2ExtTransaction(byte[] byteContent)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture) // dont seam to make any effect, so stick to default... CultureInfo.GetCultureInfo("fi-FI")
            {
                Delimiter = "\t",
            };

            string fullContent = Convert2RawString(byteContent);

            fullContent = fullContent.Replace(" ", "");     // Uses spaces inside of numbers, and things get complicated so just remove all space's
            fullContent = fullContent.Replace(",", ".");    // Uses , as decimal separator.. that fails w conversion so just replace w dots..

            using (var csv = new CsvReader(new StringReader(fullContent), config))
            {
                csv.Context.RegisterClassMap<NordnetFiCsvFormatMap>();
                return csv.GetRecords<ExtTransaction>().ToList();
            }
        }

#if false
CsvHelper has public 'Context' field in the CsvReader and there are all what needed for display a problem:

https://stackoverflow.com/questions/21609348/in-csvhelper-how-to-catch-a-conversion-error-and-know-what-field-and-what-row-it

try
{
    var records = csvReader.GetRecords<MyType>().ToList();
}
catch (CsvHelperException e)
{
    Console.WriteLine($"{e.Message} " + (e.InnerException == null ? string.Empty : e.InnerException.Message));
    Console.WriteLine($"Row: {csvReader.Context.Row}; RawLine: {csvReader.Context.RawRecord}");
    if (csvReader.Context.CurrentIndex >= 0 &&
        csvReader.Context.CurrentIndex < csvReader.Context.HeaderRecord.Length)
    {
        Console.WriteLine($"Column: {csvReader.Context.CurrentIndex}; ColumnName: {csvReader.Context.HeaderRecord[csvReader.Context.CurrentIndex]}");
    }
    throw;
}
#endif

        protected sealed class NordnetFiCsvFormatMap : ClassMap<ExtTransaction>
        {
            public NordnetFiCsvFormatMap()
            {
                Map(m => m.RecordDate).Name("Kauppapäivä").TypeConverter<NordnetFi2DateTypeConverter>();
                Map(m => m.PaymentDate).Name("Maksupäivä").TypeConverter<NordnetFi2DateTypeConverter>();
                Map(m => m.Type).Name("Tapahtumatyyppi").TypeConverter<NordnetFi2EtTypeConverter>();
                Map(m => m.Company.Ticker).Name("Arvopaperi");
                Map(m => m.Company.ISIN).Name("ISIN");
                Map(m => m.Units).Name("Määrä");
                Map(m => m.AmountPerUnit).Name("Kurssi");
                Map(m => m.Fee).Name("Kokonaiskulut");
                Map(m => m.Currency).Name("Valuutta").TypeConverter<NordnetFi2CurrencyCodeTypeConverter>();
                Map(m => m.ConversionRate).Name("Vaihtokurssi");
                Map(m => m.Note).Name("Tapahtumateksti");
                Map(m => m.UniqueID).Name("Vahvistusnumero");

#if false // As of Nordnet 2021-Aug-27th format.. they do keep changing this one and while...

Id	        Kirjauspäivä	Kauppapäivä	    Maksupäivä	Salkku	    Tapahtumatyyppi	
920609225	2021-08-18	    2021-08-18	    2021-08-20	7545734	    OSTO	

Arvopaperi	Instrumenttityyppi	ISIN	        Määrä	Kurssi	    Korko	
K	        Aktier	            CA4969024047	450	    7,3	        0	

Kokonaiskulut	Kokonaiskulut Valuutta	Summa	    Valuutta	    Hankinta-arvo	
29,6	        CAD	                    -3 314,6	CAD	            7 978,12	

Tulos	Kokonaismäärä	Saldo	    Vaihtokurssi	Tapahtumateksti	    Mitätöintipäivä	
0	    1 000	        -1 378,23	0,676			                                    

Laskelma	Vahvistusnumero	    Välityspalkkio	    Välityspalkkio Valuutta
757612324	757612324	        29,6	            CAD
#endif
            }
        }

        public class NordnetFi2EtTypeConverter : TypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                switch ( text )
                {
                    case "OSTO": return ExtTransaction.EtType.Buy;
                    case "MYYNTI": return ExtTransaction.EtType.Sell;
                    case "OSINKO": return ExtTransaction.EtType.Divident;
                    }
                return ExtTransaction.EtType.Unknown;
            }
        }

        // 

        public class NordnetFi2CurrencyCodeTypeConverter : TypeConverter
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

        public class NordnetFi2DateTypeConverter : TypeConverter
        {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
            {
                return DateTime.ParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture); // 2021-08-18
            }
        }
    }
}
