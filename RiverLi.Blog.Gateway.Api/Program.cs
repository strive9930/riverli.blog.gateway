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

            // ==========================================
            // 【关键修复：先注册 CORS 策略，再注册 YARP】
            // ==========================================

            // 1. 配置统一的全局 CORS 策略（必须在 AddReverseProxy 之前！）
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("GatewayCorsPolicy", policy =>
                {
                    policy.WithOrigins(
                            "http://192.168.16.11:30081", // 部署环境的 Vue 前端地址
                            "http://localhost:5002" // 本地开发环境的前端地址
                        )
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials(); // 允许携带 Cookie/Token 凭证
                });

                options.AddPolicy("ScalarPolicy", policy =>
                {
                    policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

            // 2. 注册 YARP 反向代理（此时 YARP 加载配置时，就能顺利找到上面注册的 "GatewayCorsPolicy" 了！）
            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            // 3. 配置全局日志与链路追踪
            builder.AddRiverLogging();
            builder.Services.AddRiverTracing("RiverLi.Gateway");

            // 4. 配置 JWT 身份验证与授权
            builder.Services.AddRiverJwtAuthentication(builder.Configuration);
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