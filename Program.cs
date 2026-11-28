using Microsoft.EntityFrameworkCore;
using HotChocolate.Data;
using RmsErp.Api.Data;
using RmsErp.Api.Queries;
using RmsErp.Api.Mutations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RmsErp.Api.Security;
using Microsoft.AspNetCore.Authorization;
using RmsErp.Api.Queries.Clientes;
using RmsErp.Api.Queries.Usuarios;
using RmsErp.Api.Queries.Menus;
using RmsErp.Api.Mutations.Clientes;
using RmsErp.Api.Mutations.Usuarios;
using RmsErp.Api.Mutations.Menus;
using RmsErp.Api.Mutations.Permisos;
using RmsErp.Api.Queries.Tracker;
using RmsErp.Api.Mutations.Tracker;
using RmsErp.Api.Queries.Contratistas;
using RmsErp.Api.Mutations.Contratistas;
using RmsErp.Api.Queries.Tareas;
using RmsErp.Api.Mutations.Tareas;
using RmsErp.Api.Queries.Cuadrillas;
using RmsErp.Api.Mutations.Cuadrillas;

// --- Usings necesarios para los archivos y el servicio ---
using RmsErp.Api.Services;
using Microsoft.Extensions.FileProviders;
using System.IO;
// ----------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Sesión: el token de Google solo se usa para iniciar sesión (/auth/sesion); luego la app usa
// el token RMS de 12 horas. El esquema "Bearer" elige cómo validar según el emisor del token.
var llaveSesion = SesionRms.Llave(builder.Configuration, builder.Environment);
builder.Services.AddSingleton(new SesionService(llaveSesion));

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = SesionRms.EsquemaPorDefecto;
        options.DefaultChallengeScheme = SesionRms.EsquemaPorDefecto;
    })
    .AddPolicyScheme(SesionRms.EsquemaPorDefecto, "Google o sesión RMS", options =>
    {
        options.ForwardDefaultSelector = SesionRms.EsquemaPara;
    })
    .AddJwtBearer(SesionRms.Esquema, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = SesionRms.Emisor,
            ValidateAudience = true,
            ValidAudience = SesionRms.Audiencia,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = llaveSesion,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    })
    .AddJwtBearer(SesionRms.EsquemaGoogle, options =>
    {
        options.Authority = "https://accounts.google.com";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "https://accounts.google.com",
            ValidateAudience = true,
            ValidAudience = "869381008070-m358g9u3unnqgo7uq13hihgpg80rganp.apps.googleusercontent.com",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(5)
        };
    });

builder.Services.AddScoped<IClaimsTransformation, ClaimsTransformer>();

builder.Services.AddAuthorization(options =>
{
    // Seguridad: además de un token válido de Google, se exige que el usuario esté registrado y ACTIVO
    // en RMS (el ClaimsTransformer solo agrega "rms_user_id" en ese caso).
    var usuarioRms = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("rms_user_id")
        .Build();

    options.DefaultPolicy = usuarioRms;   // [Authorize] sin política
    options.FallbackPolicy = usuarioRms;  // endpoints sin atributo (incluye /graphql)
});

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
    .AddTypeExtension<ClienteQuery>()
    .AddTypeExtension<UsuarioQuery>()
    .AddTypeExtension<MenuQuery>()
    .AddTypeExtension<ProyectoQuery>()
    .AddTypeExtension<SubFlujosQuery>()
    .AddTypeExtension<DashboardQuery>()
    .AddTypeExtension<ContratistaProveedorQuery>()
    .AddTypeExtension<TareaQuery>()
    .AddTypeExtension<NotificacionQuery>()
    .AddTypeExtension<CuadrillaQuery>()          // ← NUEVO
    .AddTypeExtension<RmsErp.Api.Queries.Sistema.VersionQuery>()
    .AddTypeExtension<RmsErp.Api.Queries.Sistema.PushQuery>()

    .AddMutationType(d => d.Name("Mutation"))
    .AddTypeExtension<ClienteMutation>()
    .AddTypeExtension<UsuarioMutation>()
    .AddTypeExtension<MenuMutation>()
    .AddTypeExtension<PermisoMutation>()
    .AddTypeExtension<ProyectoMutation>()
    .AddTypeExtension<SubFlujosMutation>()
    .AddTypeExtension<AnticipoDirectoMutation>()
    .AddTypeExtension<CorteFacturacionMutation>()
    .AddTypeExtension<EliminarSubFlujosMutation>()
    .AddTypeExtension<ContratistaProveedorMutation>()
    .AddTypeExtension<TareaMutation>()
    .AddTypeExtension<NotificacionMutation>()
    .AddTypeExtension<CuadrillaMutation>()       // ← NUEVO
    .AddTypeExtension<RmsErp.Api.Mutations.Sistema.PushMutation>()
    .AddTypeExtension<RmsErp.Api.Mutations.Sistema.PerfilMutation>()

    .AddAuthorization()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200", "https://rmscolombia.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// --- Registrar el servicio de Almacenamiento y los Controladores REST ---
builder.Services.AddScoped<IAlmacenamientoService, AlmacenamientoLocalService>();
builder.Services.AddControllers();
// ------------------------------------------------------------------------

// --- NUEVO: Registrar NotificacionHelperService ---
builder.Services.AddScoped<NotificacionHelperService>();

// --- Notificaciones push (Web Push con llaves VAPID; sin llaves queda desactivado) ---
var pushOpciones = builder.Configuration.GetSection("Push").Get<RmsErp.Api.Services.Push.PushOpciones>()
                   ?? new RmsErp.Api.Services.Push.PushOpciones();
builder.Services.AddSingleton(pushOpciones);
builder.Services.AddHttpClient("push", c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddSingleton<RmsErp.Api.Services.Push.WebPushEmisor>();
builder.Services.AddSingleton<RmsErp.Api.Services.Push.PushCola>();
builder.Services.AddHostedService<RmsErp.Api.Services.Push.PushWorker>();
builder.Services.AddScoped<CuadrillaKpiService>();
// --------------------------------------------------

var app = builder.Build();

// --- Configuración para servir la carpeta 'uploads' localmente ---
var uploadsPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

// Seguridad: los archivos ya no se sirven públicamente; se entregan por ArchivosController (/archivos?ruta=...)
// app.UseStaticFiles(new StaticFileOptions
// {
//     FileProvider = new PhysicalFileProvider(uploadsPath),
//     RequestPath = "/uploads"
// });
// -----------------------------------------------------------------

app.UseRouting();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapGraphQL("/graphql");

// --- Mapear los endpoints REST (UploadController) ---
app.MapControllers();
// -----------------------------------------------------

app.Run();