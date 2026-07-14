# =============================================================================
# Dockerfile — riverli-blog-gateway (API 网关)
# =============================================================================
# 构建命令（在解决方案根目录执行）：
#   docker build -t riverli-blog-gateway -f RiverLi.Blog.Gateway.Api/Dockerfile .
# 说明：本服务为单项目解决方案，无内部类库依赖，Dockerfile 结构较简单
# =============================================================================

# ─── 阶段 1：构建环境（需完整 .NET SDK，包含编译工具链） ───
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# 提前复制 .csproj 文件以实现 Docker 层缓存优化
# 仅当 .csproj 变更时才重新执行 restore，源码变更不会触发重复 restore
COPY ["RiverLi.Blog.Gateway.Api/RiverLi.Blog.Gateway.Api.csproj", "RiverLi.Blog.Gateway.Api/"]

# 恢复 NuGet 依赖包（包括 RiverLi.Blog.Infrastructure.Shared v1.0.3）
RUN dotnet restore "RiverLi.Blog.Gateway.Api/RiverLi.Blog.Gateway.Api.csproj"

# 复制整个解决方案的源代码
COPY . .

# 切换到 Api 项目目录并发布（Release 模式，输出到 /app/publish）
WORKDIR "/src/RiverLi.Blog.Gateway.Api"
RUN dotnet publish "RiverLi.Blog.Gateway.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ─── 阶段 2：极简运行环境（仅包含 ASP.NET 运行时，镜像体积 < 100MB） ───
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# 暴露 8080 端口（K8s Service 会通过 targetPort 将流量转发到此端口）
EXPOSE 8080

# 从构建阶段复制发布产物
COPY --from=build /app/publish .

# 容器启动入口
ENTRYPOINT ["dotnet", "RiverLi.Blog.Gateway.Api.dll"]
