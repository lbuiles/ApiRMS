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

// --- Usings necesarios para los archivos y el servicio ---
using RmsErp.Api.Services;
using Microsoft.Extensions.FileProviders;
using System.IO;
// ----------------------------------------------------------

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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
    options.FallbackPolicy = null;

    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
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
    .AddTypeExtension<NotificacionQuery>()          // ← NUEVO

    .AddMutationType(d => d.Name("Mutation"))
    .AddTypeExtension<ClienteMutation>()
    .AddTypeExtension<UsuarioMutation>()
    .AddTypeExtension<MenuMutation>()
    .AddTypeExtension<PermisoMutation>()
    .AddTypeExtension<ProyectoMutation>()
    .AddTypeExtension<SubFlujosMutation>()
    .AddTypeExtension<AnticipoDirectoMutation>()
    .AddTypeExtension<EliminarSubFlujosMutation>()
    .AddTypeExtension<ContratistaProveedorMutation>()
    .AddTypeExtension<TareaMutation>()
    .AddTypeExtension<NotificacionMutation>()       // ← NUEVO

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
// --------------------------------------------------

var app = builder.Build();

// --- Configuración para servir la carpeta 'uploads' localmente ---
var uploadsPath = System.IO.Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});
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