using ExperimentSimulation.BusinessLayer.Abstract;
using ExperimentSimulation.BusinessLayer.Concrete;
using ExperimentSimulation.DataAccessLayer.Abstract;
using ExperimentSimulation.DataAccessLayer.EntityFramework;
using ExperimentSimulation.DataAccessLayer.Concrete;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5156");

builder.Services.AddDbContext<Context>(options =>
{
    var cs = "Server=localhost;Database=ExperimentSimulation;Uid=root;Pwd=0000";
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

builder.Services.AddScoped<IUserDal, EfUserDal>();
builder.Services.AddScoped<IUserService, UserManager>();
builder.Services.AddScoped<IRoleDal, EfRoleDal>();
builder.Services.AddScoped<IRoleService, RoleManager>();

builder.Services.AddCors(o =>
{
    o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");


app.UseAuthorization();

app.MapControllers();
app.Run();
