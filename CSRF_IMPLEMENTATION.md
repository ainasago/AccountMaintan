# CSRF 保护实现说明

## 概述

本项目实现了完整的CSRF（跨站请求伪造）保护机制，包括中间件、服务、API端点和前端集成。

## 实现组件

### 1. CSRF保护中间件 (`src/Middleware/CsrfProtectionMiddleware.cs`)

#### 功能特性
- **自动验证**：对所有API请求自动进行CSRF令牌验证
- **智能跳过**：自动跳过GET请求、静态文件和Razor Pages请求
- **双重验证**：同时验证请求头和Cookie中的令牌
- **安全日志**：记录所有CSRF验证失败事件

#### 核心逻辑
```csharp
// 只对API请求进行CSRF检查
if (IsApiRequest(context.Request.Path))
{
    // 检查CSRF令牌
    if (!await ValidateCsrfToken(context))
    {
        // 返回403错误
        context.Response.StatusCode = 403;
        // 返回JSON错误信息
    }
}
```

#### 验证流程
1. 从请求头获取 `X-CSRF-TOKEN`
2. 从Cookie获取 `CSRF-TOKEN`
3. 比较两个令牌是否一致
4. 验证令牌格式（64位十六进制字符串）
5. 记录验证结果

### 2. CSRF令牌服务 (`src/Middleware/CsrfProtectionMiddleware.cs`)

#### 接口定义
```csharp
public interface ICsrfTokenService
{
    string GenerateToken();
    void SetTokenCookie(HttpContext context, string token);
}
```

#### 实现特性
- **安全生成**：使用 `RandomNumberGenerator` 生成32字节随机数
- **十六进制编码**：转换为64位十六进制字符串
- **Cookie设置**：HttpOnly、Secure、SameSite=Strict
- **过期时间**：1小时自动过期

### 3. CSRF令牌API (`src/Controllers/AdminController.cs`)

#### API端点
```
GET /api/admin/csrf-token
```

#### 功能
- 生成新的CSRF令牌
- 设置CSRF令牌Cookie
- 返回令牌给前端使用
- 支持匿名访问

#### 响应格式
```json
{
    "success": true,
    "token": "a1b2c3d4e5f6..."
}
```

### 4. 前端集成

#### 初始化流程
1. 页面加载时自动获取CSRF令牌
2. 将令牌存储在全局变量中
3. 所有AJAX请求自动添加令牌头

#### 实现代码
```javascript
// 初始化CSRF令牌
async function initializeCsrfToken() {
    const response = await fetch('/api/admin/csrf-token', {
        method: 'GET',
        credentials: 'include'
    });
    
    if (response.ok) {
        const data = await response.json();
        if (data.success) {
            csrfToken = data.token;
        }
    }
}

// 获取CSRF令牌（如果不存在则重新获取）
async function getCsrfToken() {
    if (!csrfToken) {
        await initializeCsrfToken();
    }
    return csrfToken;
}
```

#### 请求头设置
```javascript
const response = await fetch('/api/notes', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'X-CSRF-TOKEN': await getCsrfToken()
    },
    credentials: 'include',
    body: JSON.stringify(data)
});
```

## 安全特性

### 1. 令牌生成
- **加密强度**：使用系统级随机数生成器
- **唯一性**：每次生成都是唯一的64位十六进制字符串
- **不可预测**：基于加密安全的随机数

### 2. 令牌存储
- **HttpOnly**：防止JavaScript访问Cookie
- **Secure**：HTTPS环境下才传输
- **SameSite=Strict**：防止跨站请求
- **过期时间**：1小时后自动失效

### 3. 验证机制
- **双重验证**：同时验证请求头和Cookie
- **格式检查**：验证令牌格式和长度
- **精确匹配**：使用 `StringComparison.Ordinal` 精确比较

### 4. 错误处理
- **详细日志**：记录验证失败的原因和来源IP
- **统一响应**：返回标准的JSON错误格式
- **安全响应**：不泄露敏感信息

## 使用场景

### 1. 自动保护
- 所有API请求自动受到保护
- 无需手动添加验证代码
- 中间件自动处理验证逻辑

### 2. 前端集成
- 自动获取和刷新令牌
- 所有AJAX请求自动添加令牌
- 支持文件上传等复杂请求

### 3. 开发友好
- 提供调试日志
- 清晰的错误信息
- 简单的API接口

## 配置说明

### 1. 服务注册 (`src/Program.cs`)
```csharp
builder.Services.AddScoped<ICsrfTokenService, CsrfTokenService>();
```

### 2. 中间件注册 (`src/Program.cs`)
```csharp
app.UseMiddleware<CsrfProtectionMiddleware>();
```

### 3. 中间件顺序
```csharp
app.UseStaticFiles();
app.UseMiddleware<ApiSecurityMiddleware>();
app.UseMiddleware<CsrfProtectionMiddleware>();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
```

## 测试验证

### 1. 正常请求
- 包含有效CSRF令牌的请求应该成功
- 令牌在Cookie和请求头中都应该存在

### 2. 无效请求
- 缺少CSRF令牌的请求应该返回403
- 令牌不匹配的请求应该返回403
- 格式错误的令牌应该返回403

### 3. 绕过测试
- GET请求应该被跳过
- 静态文件请求应该被跳过
- Razor Pages请求应该被跳过

## 最佳实践

### 1. 前端开发
- 始终使用 `getCsrfToken()` 获取令牌
- 在所有API请求中包含令牌头
- 处理令牌过期的情况

### 2. 后端开发
- 不要绕过CSRF保护
- 使用标准的API响应格式
- 记录安全事件

### 3. 安全考虑
- 定期轮换令牌
- 监控验证失败事件
- 考虑添加速率限制

## 故障排除

### 1. 常见问题
- **令牌过期**：重新获取令牌
- **格式错误**：检查令牌生成逻辑
- **Cookie问题**：检查Cookie设置

### 2. 调试方法
- 查看浏览器开发者工具的网络请求
- 检查服务器日志
- 验证Cookie是否正确设置

### 3. 性能考虑
- 令牌生成开销很小
- 验证逻辑简单高效
- 内存使用最小化

## 总结

本项目的CSRF保护实现提供了完整的安全防护，包括：

1. **自动保护**：中间件自动保护所有API请求
2. **安全令牌**：使用加密安全的随机数生成令牌
3. **双重验证**：同时验证请求头和Cookie中的令牌
4. **前端集成**：自动获取和使用CSRF令牌
5. **详细日志**：记录所有安全事件
6. **开发友好**：提供简单的API和清晰的错误信息

这个实现确保了应用程序免受CSRF攻击，同时保持了良好的开发体验。
