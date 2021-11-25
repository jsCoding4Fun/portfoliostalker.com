
namespace PFS.Shared.UiTypes
{
    // These are properties that user itself has possibility to control
    public enum UserSettProperty : int
    {
        Unknown = 0,

        NoLocalStorage,     // Default is that data is stored LocalStorage, with "TRUE" user can prevent it on settings (visibility controlled by AdminProp!) 
        BackupPrivateKeys,  // Fully controls if PFS, PrivSrv or LocalBackups contain private provider keys or not (always available option)
    }

    // These are properties to specific user that only admin can control (some are readonly == RO)
    public enum UserSettPropertyAdmin : int
    {
        Unknown = 0,

        UsernameHashRO,
        EmailHashRO,
        AccountTypeRO,

        FeaKirkVslImport,               // Limited edition import functionality, only few users need this
        FeaAdminReports,                // Send special news article daily for fea owners from critical events / general stats on servers

        ActUserPropNoLocalStorage,      // Activation for User Property Feature "NoLocalStorage", so admin needs to grand this so that user has option available
    }
}
