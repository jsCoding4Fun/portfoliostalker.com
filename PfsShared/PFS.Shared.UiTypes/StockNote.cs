using System.Linq;

namespace PFS.Shared.UiTypes
{
    // Each and every stock can have one note -article attached to it per account/person
    public class StockNote
    {
        public string Overview { get; set; }

        public string BodyText { get; set; }

        public string[] Groups { get; set; }        // If specific stock has group selection done, then this contains selected group field name

        public string[] GroupHeader { get; set; }   // ReadOnly, has TagGroup headers for viewing. Contains *null* if group is N/A

        public StockNote()
        {
            Overview = string.Empty;
            BodyText = string.Empty;
            Groups = new string[TagGroupsUsage.MaxTagGroups];
            GroupHeader = new string[TagGroupsUsage.MaxTagGroups];
        }

        // For public reports, by adding overview or bodyText a [* text here, multilines ok *] can fetch only that text to be shown on reports
        public string GetReportNote()
        {
            int start;
            int end;

            // Overview is higher priority

            if ( string.IsNullOrWhiteSpace(Overview) == false )
            {
                start = Overview.IndexOf("[*");
                end = Overview.IndexOf("*]");

                if ( start >= 0 && end >= 0 && end - start >= 5 )
                    return Overview.Substring(start + 2, end - start - 2);
            }

            // BodyText is second priority

            if (string.IsNullOrWhiteSpace(BodyText) == false)
            {
                start = BodyText.IndexOf("[*");
                end = BodyText.IndexOf("*]");

                if (start >= 0 && end >= 0 && end - start >= 5)
                    return BodyText.Substring(start + 2, end - start - 2);
            }

            return string.Empty;
        }

        public bool IsEmpty()
        {
            if (string.IsNullOrWhiteSpace(Overview) == false
              || string.IsNullOrWhiteSpace(BodyText) == false)
                return false;

            if (Groups.Where(s => string.IsNullOrEmpty(s) == false).Count() > 0)
                return false;

            return true;
        }

        public string IsValid()
        {
            string err;

            err = IsValidOverview();
            if (string.IsNullOrEmpty(err) == false)
                return err;

            err = IsValidBody();
            if (string.IsNullOrEmpty(err) == false)
                return err;

            return string.Empty;
        }

        public string IsValidOverview()
        {
            // Limit to 1000 characters
            // Limit character set

            return string.Empty;
        }

        public string IsValidBody()
        {
            return string.Empty;
        }
    }
}
