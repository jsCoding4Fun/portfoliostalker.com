using System;

namespace PFS.Shared.Types
{
    // Format that PFS.External.Transaction providers convert Broker records, for processing toward PFS.Client Stalker usage
    public class ExtTransaction
    {
        public EtCompany    Company { get; set; }           // Filled per available information from transaction (and stays like that, so limited use for viewing)

        public string       UniqueID { get; set; }          // Must be set if wanting to avoid duplicates on rerun's
        public EtType       Type { get; set; }
        public DateTime     RecordDate { get; set; }        // Actual buy/sell date, or for dividents a day when divident is locked for companys owner
        public DateTime     PaymentDate { get; set; }       // When transaction finished so 2 days delay for buy/sell. For divident is when company pays it.

        public decimal      Units { get; set; }
        public decimal      AmountPerUnit { get; set; }     // PricePerUnit, PaymentPerUnit, etc ... generically AmountPerUnit as usage depends of Type
        public decimal      Fee { get; set; }

        public CurrencyCode Currency { get; set; }
        public decimal      ConversionRate { get; set; }
        public string       Note { get; set; }

        // To be filled on processing of EtRecord

        public ProcessingStatus Status { get; set; } = ProcessingStatus.Unknown;

        public enum ProcessingStatus
        {
            Unknown = 0,
            Acceptable,
            Duplicate,
            Error_UnknownRecordType,
            Error_UnitAmount,
            Error_PricePerUnit,
            Error_Fee,
        }

        public enum EtType : int
        {
            Unknown = 0,
            Buy,
            Sell,
            Divident,
        }

        public ExtTransaction()
        {
            Company = new();
        }
    }


    public class EtCompany
    {
        public string Ticker { get; set; }  // Filled by converter provided information, that mostly very partial depending provider
        public string Market { get; set; }
        public string Name { get; set; }
        public string ISIN { get; set; }

        public Guid STID { get; set; } = Guid.Empty;    // Initially set Empty, but filled w PFS value when match to Stalker is found
    }
}
