using RiverLi.Blog.Infrastructure.Shared.Extensions;
using RiverLi.Blog.Infrastructure.Shared.Logging;
using Scalar.AspNetCore;

namespace RiverLi.Blog.Gateway.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            // 1. 注册 YARP 反向代理
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            // 2. 配置 CORS - 开发环境允许所有来源
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("ScalarPolicy", policy =>
                {
                    policy.AllowAnyOrigin() // 调试阶段先用 AllowAnyOrigin
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
                options.AddPolicy("DefaultPolicy", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
                options.AddPolicy("DefaultPolicy", policy =>
                {
                    policy.WithOrigins("http://localhost:5002") // 允许来自 Blog UI 的请求
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials(); // 如果涉及 Cookie 或特定身份凭证
                });
                // CORS 配置
                options.AddPolicy("CorsPolicy", policy =>
                {
                    policy.WithOrigins("http://192.168.16.11:30081") // 你的前端地址
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials();
                });
            });

            // 3. 配置全局日志
            builder.AddRiverLogging();
            builder.Services.AddRiverTracing("RiverLi.Gateway");

            // 4. 配置 JWT 身份验证
            builder.Services.AddRiverJwtAuthentication(builder.Configuration);
            
            // 5. 配置授权
            builder.Services.AddAuthorization();

            // 网关不需要 AddOpenApi(除非网关自己有接口)，直接配置 Scalar 即可
            var app = builder.Build();
            
            // ========== 中间件顺序（非常重要！）==========
            
            // 1. 日志中间件（最先）
            app.UseMiddleware<RiverRequestLoggingMiddleware>();
            
            // 2. CORS 必须在路由和认证之前
            app.UseCors("DefaultPolicy");
            app.UseCors("ScalarPolicy");
            app.UseCors("CorsPolicy");
            // 3. HTTPS 重定向
            if (!app.Environment.IsDevelopment())
            {
                app.UseHttpsRedirection();
            }
            
            // 4. 路由匹配
            app.UseRouting();
            
            // 5. 认证（解析 Token，设置 User.Identity）
            app.UseAuthentication();
            
            // 6. 授权（检查用户权限）
            app.UseAuthorization();
            
            // 7. 端点映射
            // 映射反向代理（只调用一次！）
            app.MapReverseProxy();
            // 映射控制器
            app.MapControllers();
            
            // 8. 开发环境特性
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.Run();
        }
    }
}