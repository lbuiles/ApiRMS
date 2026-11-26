using System.Reflection;
using HotChocolate.Authorization;

namespace RmsErp.Api.Queries.Sistema;

/// <summary>Fecha de compilación (UTC, ISO 8601) y commit corto de la API publicada.</summary>
public record VersionApi(string? Fecha, string? Commit);

[ExtendObjectType("Query")]
public class VersionQuery
{
    private static readonly VersionApi Info = Leer();

    [Authorize]
    public VersionApi GetVersionApi() => Info;

    private static VersionApi Leer()
    {
        var asm = typeof(VersionQuery).Assembly;

        // La agrega el .csproj en cada compilación (AssemblyMetadata "FechaCompilacion")
        var fecha = asm.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "FechaCompilacion")?.Value;

        // El SDK de .NET agrega el commit de git a la versión informativa: "1.0.0+<sha>"
        var informativa = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        string? commit = null;
        var i = informativa?.IndexOf('+') ?? -1;
        if (i >= 0)
        {
            commit = informativa![(i + 1)..];
            if (commit.Length > 7) commit = commit[..7];
        }

        return new VersionApi(fecha, commit);
    }
}
