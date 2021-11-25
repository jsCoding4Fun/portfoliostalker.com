using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using PFS.Shared.Types;

namespace PFS.Shared.Common
{
    public class XmlMarketMeta
    {
        static public string ExportXML(List<MarketMeta> allMarkets)
        {
            XElement rootPFS = new XElement("MARKETS");

            foreach (MarketMeta meta in allMarkets )
            {
                XElement marketElem = new XElement(meta.ID.ToString());
                marketElem.SetAttributeValue("MIC", meta.MIC);
                marketElem.SetAttributeValue("name", meta.Name);
                marketElem.SetAttributeValue("currency", meta.Currency.ToString());
                marketElem.SetAttributeValue("marketLocalClosingHour", meta.MarketLocalClosingHour);
                marketElem.SetAttributeValue("marketLocalClosingMin", meta.MarketLocalClosingMin);
                marketElem.SetAttributeValue("linuxTag", meta.LinuxTag);
                marketElem.SetAttributeValue("wasmTag", meta.WasmTag);
                marketElem.SetAttributeValue("marketToUTC", meta.MarketLocalToUtc);

                rootPFS.Add(marketElem);
            }
            return rootPFS.ToString();
        }

        static public List<MarketMeta> ImportXML(string xml)
        {
            try
            {
                XDocument xmlDoc = XDocument.Parse(xml);

                return (from e in xmlDoc.Element("MARKETS").Elements()

                        select new MarketMeta()
                        {
                            ID = (MarketID)Enum.Parse(typeof(MarketID), (string)e.Name.ToString()),
                            MIC = (string)e.Attribute("MIC"),
                            Name = (string)e.Attribute("name"),
                            Currency = (CurrencyCode)Enum.Parse(typeof(CurrencyCode), (string)e.Attribute("currency")),
                            MarketLocalClosingHour = (int)e.Attribute("marketLocalClosingHour"),
                            MarketLocalClosingMin = e.Attribute("marketLocalClosingMin") != null ? (int)e.Attribute("marketLocalClosingMin") : 0,
                            LinuxTag = (string)e.Attribute("linuxTag"),
                            WasmTag = (string)e.Attribute("wasmTag"),
                            MarketLocalToUtc = (int)e.Attribute("marketToUTC"),
                        }).ToList();
            }
            catch (Exception)
            {
            }
            return null;
        }
    }
}
