
namespace PFS.Shared.Types
{
    public enum StalkerError : int
    {
        Unknown = 0,
        OK,
        FAIL,
        NotSupported,
        Duplicate,
        TooManyParametes,
        InvalidParameter,
        InvalidReference,
        MissingParameters,
        OutOfRangeParameter,
        InvalidCharSet,
        UnTrackedStock,
        CantDeleteNotEmpty,
        SellingUnitsMoreThanOwns,

        NameTooLong,
        NameInvalidCharacters,
    }
}
