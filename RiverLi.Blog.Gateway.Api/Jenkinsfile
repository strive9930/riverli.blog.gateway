// =============================================================================
// Jenkins Pipeline — riverli-blog-gateway (API 网关)
// =============================================================================
// 用途：自动构建 Docker 镜像 → 推送到 GitHub Container Registry → 更新 K8s 配置库
// 触发：Jenkins 检测到 Git 仓库变更时自动执行
// 对应 K8s 编排文件：apps/gateway.yaml
// =============================================================================

pipeline {
    // 🌟 允许在任何可用的 Jenkins agent 上运行
    agent any

    // ─── 全局环境变量 ───
    environment {
        // GitHub 容器镜像仓库地址（不含标签）
        IMAGE_NAME = "ghcr.io/strive9930/riverli-blog-gateway"
        // 利用 Jenkins 内置的 BUILD_NUMBER 生成唯一版本号，例如 v1, v2, v3...
        IMAGE_TAG = "v${BUILD_NUMBER}"
        // K8s 清单配置库的 Git 地址（不含凭据前缀）
        MANIFEST_REPO = "github.com/strive9930/riverli-k8s-manifests.git"
    }

    stages {
        // ─── 阶段 1：检出业务代码 ───
        stage('📥 1. 检出业务代码') {
            steps {
                echo "开始构建微服务: ${IMAGE_NAME}:${IMAGE_TAG}"
                // Jenkins 默认会自动 checkout 当前仓库的代码，无需额外指令
            }
        }

        // ─── 阶段 2：构建并推送 Docker 镜像 ───
        stage('📦 2. 构建并推送公共镜像') {
            steps {
                script {
                    // 🔨 构建带版本号的镜像（build context = 解决方案根目录）
                    sh "docker build -t ${IMAGE_NAME}:${IMAGE_TAG} -f RiverLi.Blog.Gateway.Api/Dockerfile ."
                    // 🔨 同时构建 latest 标签，方便开发环境快速拉取
                    sh "docker build -t ${IMAGE_NAME}:latest -f RiverLi.Blog.Gateway.Api/Dockerfile ."

                    // 🔐 登录 GitHub Container Registry 并推送两个标签
                    withCredentials([usernamePassword(
                        credentialsId: 'github-registry-credentials',
                        usernameVariable: 'REG_USER',
                        passwordVariable: 'REG_PASS'
                    )]) {
                        sh 'echo $REG_PASS | docker login ghcr.io -u $REG_USER --password-stdin'
                        sh "docker push ${IMAGE_NAME}:${IMAGE_TAG}"
                        sh "docker push ${IMAGE_NAME}:latest"
                    }
                }
            }
        }

        // ─── 阶段 3：GitOps — 跨仓库改写 K8s 配置库 ───
        stage('🔄 3. 跨仓库改写 K8s 配置库 (GitOps 核心)') {
            steps {
                script {
                    // 清理上次构建遗留的临时目录
                    sh 'rm -rf manifest-folder'

                    // 克隆 K8s 配置库（使用 GitHub Token 认证）
                    withCredentials([string(credentialsId: 'github-token', variable: 'GITHUB_TOKEN')]) {
                        sh 'git clone https://$GITHUB_TOKEN@github.com/strive9930/riverli-k8s-manifests.git manifest-folder'
                    }

                    dir('manifest-folder') {
                        // 🌟 精准改写 gateway 编排文件中的镜像版本号
                        // 将 image: ghcr.io/.../riverli-blog-gateway:xxx 替换为当前版本
                        sh "sed -i 's|image: ${IMAGE_NAME}:.*|image: ${IMAGE_NAME}:${IMAGE_TAG}|g' apps/gateway.yaml"

                        // 提交并推送变更（ArgoCD 或其他 GitOps 工具会自动同步到集群）
                        sh 'git config user.name "Jenkins CI"'
                        sh 'git config user.email "jenkins@riverli.com"'
                        sh 'git add apps/gateway.yaml'
                        sh "git commit -m '🤖 Jenkins 自动触发发版: ${IMAGE_NAME}:${IMAGE_TAG}'"

                        withCredentials([string(credentialsId: 'github-token', variable: 'GITHUB_TOKEN')]) {
                            sh 'git push https://$GITHUB_TOKEN@github.com/strive9930/riverli-k8s-manifests.git main'
                        }
                    }
                }
            }
        }
    }
}
