# 内容安全策略(CSP)配置指南

## 概述

本项目已实现基于配置文件的内容安全策略(CSP)管理，支持开发环境和生产环境的不同配置。

## 配置文件结构

### appsettings.json (生产环境)
```json
{
  "Security": {
    "ContentSecurityPolicy": {
      "DefaultSrc": "'self'",
      "ScriptSrc": "'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com",
      "StyleSrc": "'self' 'unsafe-inline' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com",
      "ImgSrc": "'self' data: https:",
      "FontSrc": "'self' data: https://cdn.jsdelivr.net https://cdnjs.cloudflare.com https://at.alicdn.com",
      "ConnectSrc": "'self' wss: ws: http://localhost:*",
      "FrameAncestors": "'none'",
      "BaseUri": "'self'",
      "FormAction": "'self'",
      "ObjectSrc": "'none'",
      "MediaSrc": "'self'",
      "ManifestSrc": "'self'",
      "WorkerSrc": "'self'",
      "ChildSrc": "'self'",
      "FrameSrc": "'none'",
      "UpgradeInsecureRequests": false
    }
  }
}
```

### appsettings.Development.json (开发环境)
```json
{
  "Security": {
    "Development": {
      "ContentSecurityPolicy": {
        "DefaultSrc": "'self' 'unsafe-inline' 'unsafe-eval' data: https:",
        "ScriptSrc": "'self' 'unsafe-inline' 'unsafe-eval' https:",
        "StyleSrc": "'self' 'unsafe-inline' https:",
        "ImgSrc": "'self' data: https:",
        "FontSrc": "'self' data: https:",
        "ConnectSrc": "'self' wss: ws: http: https:",
        "FrameAncestors": "'none'",
        "BaseUri": "'self'",
        "FormAction": "'self'",
        "ObjectSrc": "'none'",
        "MediaSrc": "'self'",
        "ManifestSrc": "'self'",
        "WorkerSrc": "'self'",
        "ChildSrc": "'self'",
        "FrameSrc": "'none'",
        "UpgradeInsecureRequests": false
      }
    }
  }
}
```

## CSP指令说明

### 基础指令
- **default-src**: 默认源，当其他指令未指定时的回退值
- **script-src**: 控制JavaScript文件的加载和执行
- **style-src**: 控制CSS样式表的加载
- **img-src**: 控制图片资源的加载
- **font-src**: 控制字体文件的加载

### 连接和媒体指令
- **connect-src**: 控制AJAX、WebSocket等连接
- **media-src**: 控制音频和视频资源
- **object-src**: 控制插件（如Flash）的加载
- **child-src**: 控制子窗口和Worker的创建

### 导航和表单指令
- **frame-ancestors**: 控制哪些页面可以嵌入当前页面
- **base-uri**: 控制base标签的href属性
- **form-action**: 控制表单提交的目标

### 特殊指令
- **upgrade-insecure-requests**: 自动将HTTP请求升级为HTTPS
- **block-all-mixed-content**: 阻止所有混合内容

## 源值说明

### 关键字
- **'self'**: 同源
- **'unsafe-inline'**: 允许内联脚本和样式（不推荐）
- **'unsafe-eval'**: 允许eval()等动态代码执行（不推荐）
- **'none'**: 禁止所有源
- **'strict-dynamic'**: 信任动态加载的脚本

### 协议
- **data:**: 允许data: URL
- **blob:**: 允许blob: URL
- **https:**: 允许HTTPS协议
- **http:**: 允许HTTP协议（不推荐）

### 通配符
- **\***: 允许所有源（不推荐）
- **\*.example.com**: 允许example.com的所有子域名

## 安全建议

### 1. 避免使用unsafe-inline
```json
// 不推荐
"ScriptSrc": "'self' 'unsafe-inline'"

// 推荐：使用nonce
"ScriptSrc": "'self' 'nonce-{random}'"
```

### 2. 避免使用unsafe-eval
```json
// 不推荐
"ScriptSrc": "'self' 'unsafe-eval'"

// 推荐：移除unsafe-eval
"ScriptSrc": "'self'"
```

### 3. 使用具体的域名
```json
// 不推荐
"ScriptSrc": "'self' https:"

// 推荐：使用具体域名
"ScriptSrc": "'self' https://cdn.jsdelivr.net https://cdnjs.cloudflare.com"
```

### 4. 添加重要的安全指令
```json
{
  "FrameAncestors": "'none'",  // 防止点击劫持
  "BaseUri": "'self'",         // 限制base标签
  "ObjectSrc": "'none'",       // 禁用插件
  "UpgradeInsecureRequests": true  // 升级到HTTPS
}
```

## 测试和验证

### 1. CSP测试页面
访问 `/CspTest` 页面进行CSP功能测试。

### 2. API端点
- `GET /api/csp/config`: 获取当前CSP配置
- `POST /api/csp/validate`: 验证CSP配置
- `POST /api/csp/recommendations`: 获取CSP建议
- `POST /api/csp/test-violation`: 测试CSP违规

### 3. 浏览器开发者工具
在浏览器开发者工具的Console中查看CSP违规报告。

## 常见问题

### 1. 内联样式不生效
**问题**: 内联样式被CSP阻止
**解决**: 在StyleSrc中添加'unsafe-inline'或使用nonce

### 2. 外部资源加载失败
**问题**: CDN资源无法加载
**解决**: 在对应指令中添加CDN域名

### 3. AJAX请求失败
**问题**: AJAX请求被CSP阻止
**解决**: 在ConnectSrc中添加目标域名

### 4. 内联脚本不执行
**问题**: 内联脚本被CSP阻止
**解决**: 在ScriptSrc中添加'unsafe-inline'或使用nonce

## 最佳实践

1. **最小权限原则**: 只允许必要的源
2. **定期审查**: 定期检查CSP配置的有效性
3. **测试覆盖**: 确保所有功能在CSP下正常工作
4. **监控违规**: 监控CSP违规报告
5. **渐进增强**: 逐步收紧CSP策略

## 参考资料

- [MDN Content Security Policy](https://developer.mozilla.org/en-US/docs/Web/HTTP/CSP)
- [CSP Evaluator](https://csp-evaluator.withgoogle.com/)
- [CSP Test](https://csp-test.com/)
