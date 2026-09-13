namespace PropFlow.Application;

// PF-S01.04: the catalog a role-matrix admin screen needs to render every role, not just the
// ones an organization currently has members in. Keep this list aligned with every named role
// arm in Capabilities.ForRole; portal and owner roles are valid matrix roles even when they do
// not currently have a member in the organization.
public static class Roles
{
    public const string OrganizationAdmin = "Organization Admin";
    public const string PropertyManager = "Property Manager";
    public const string RegionalManager = "Regional Manager";
    public const string MaintenanceSupervisor = "Maintenance Supervisor";
    public const string ReadOnly = "Read Only";
    public const string Technician = "Technician";
    public const string Vendor = "Vendor";
    public const string Owner = "Owner";
    public const string Resident = "Resident";

    public static readonly IReadOnlyList<string> All =
        [OrganizationAdmin, PropertyManager, RegionalManager, MaintenanceSupervisor, ReadOnly, Technician, Vendor, Owner, Resident];
}
