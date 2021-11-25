
namespace PFS.Shared.UiTypes
{
    // These properties/commands allow to control behaviour of Priv Server
    public enum PrivSrvProperty : int
    {
        Unknown = 0,
        AuthenticatedRO,        // If set TRUE then this is PFS authenticated official server (read only)

        NewStockProvider,       // Requires 'ExtDataProviders' exactly as enum defines, or defaults to unknown
        NewStockFromYYMMDD,     // Default start date to be used for fetch period when adding new stocks to PrivSrv
        StartupCreateJobs,      // If set to TRUE then creates each reboot of privsrv a fetch jobs for missing stocks
        AllowUrlCommands,       // If set to TRUE then allows PrivSrv to take some speed commands directly from URL 
        LimitGoldStockMax,      // If set then limits gold accounts total supported stock amount
        LimitPlatinumStockMax,  //

        // These all needs to start w 'Cmd'
        CmdGetStatus,           // Command, no parameters. Returns list of all tracked stocks, and their EOD / Job status
        CmdForceUpdate,         // Command, no parameters. Allows admin to force fetch of missing stocks latest data
        CmdForceUpdateSoloJobs, // Command, no parameters. As CmdForceUpdate but all jobs always as separate SOLO jobs 
        CmdGetProviderLog,      // Command, no parameters. See whats up w providers (returns only last N entries max)
    }
}
