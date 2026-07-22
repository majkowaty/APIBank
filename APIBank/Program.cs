var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

builder.Services.AddScoped<ITransactionService, PbTransactionService>();
builder.Services.AddHttpClient<OutsideTransactionService>(client =>
{
    client.BaseAddress = new Uri("http://majko.ddns.net:9090");
    client.Timeout = TimeSpan.FromSeconds(10);
});
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(c => {c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bank API"); 
    c.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
