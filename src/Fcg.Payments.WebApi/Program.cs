using Fcg.Payments.Application;
using Fcg.Payments.Domain;
using Fcg.Payments.Domain._Shared.Modules;
using Fcg.Payments.Infrastructure;
using Fcg.Payments.WebApi;
using Fcg.Payments.WebApi._Shared.Database;
using Fcg.Payments.WebApi._Shared.Endpoints;
using Fcg.Payments.WebApi._Shared.HealthChecks;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration));

builder.Services
    .AddModule<FcgPaymentsDomainModule>(builder.Configuration)
    .AddModule<FcgPaymentsApplicationModule>(builder.Configuration)
    .AddModule<FcgPaymentsInfrastructureModule>(builder.Configuration)
    .AddModule<FcgPaymentsWebApiModule>(builder.Configuration);

var app = builder.Build();

await app.ApplyMigrationsAsync(app.Lifetime.ApplicationStopping);

app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapHealthEndpoints();
app.MapEndpoints();

await app.RunAsync();

public partial class Program;
