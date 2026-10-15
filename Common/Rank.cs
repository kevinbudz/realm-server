namespace RotMG.Common
{
    //[W11] Account authority. Levels are compared with >=. Donor covers stored values 10 to 40, so any value in
    //that span is a donor. Commands gated by the old Ranked flag use Admin. Mod and Dev are defined for later
    //commands and are not used by any command yet.
    public enum Rank
    {
        Player = 0,
        Donor = 10,
        Mod = 80,
        Dev = 90,
        Admin = 100
    }
}
