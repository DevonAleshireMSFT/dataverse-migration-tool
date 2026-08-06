namespace DataverseMigrationTool.Domain.Enums;

/// <summary>
/// Dataverse cloud partitions supported by environment profiles.
/// </summary>
public enum DataverseCloud
{
    /// <summary>
    /// Commercial public cloud.
    /// </summary>
    Public,

    /// <summary>
    /// Microsoft GCC cloud using public Entra authority endpoints.
    /// </summary>
    Gcc,

    /// <summary>
    /// Microsoft GCC High sovereign cloud.
    /// </summary>
    GccHigh,

    /// <summary>
    /// Microsoft Department of Defense sovereign cloud.
    /// </summary>
    Dod
}
