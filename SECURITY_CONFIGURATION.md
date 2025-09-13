# 安全配置指南

## 环境变量配置

### 必需的环境变量

```bash
# 加密密钥（必须设置，用于数据加密）
ENCRYPTION_KEY=your-very-secure-encryption-key-here-32-chars-min

# 数据库连接字符串（可选，默认使用SQLite）
CONNECTION_STRING=Data Source=accounts.db

# 是否启用HTTPS（生产环境必须）
ASPNETCORE_URLS=https://localhost:5001;http://localhost:5000
```

### 推荐的生产环境配置

```bash
# 加密密钥（使用强随机密钥）
ENCRYPTION_KEY=$(openssl rand -base64 32)

# 数据库连接字符串（使用PostgreSQL或SQL Server）
CONNECTION_STRING="Host=localhost;Database=accountmanager;Username=user;Password=password"

# 强制HTTPS
ASPNETCORE_URLS=https://0.0.0.0:443
ASPNETCORE_Kestrel__Certificates__Default__Path=/path/to/certificate.pfx
ASPNETCORE_Kestrel__Certificates__Default__Password=certificate-password
```

## 安全功能说明

### 1. 密码策略
- **最小长度**: 12个字符
- **字符要求**: 必须包含大小写字母、数字和特殊字符
- **唯一字符**: 至少4个不同字符
- **弱密码检查**: 自动检测常见弱密码
- **模式检查**: 防止键盘模式（如qwerty）

### 2. 加密机制
- **算法**: AES-256-CBC
- **密钥派生**: PBKDF2 with SHA-256
- **IV生成**: 每次加密生成新的随机IV
- **密钥管理**: 使用环境变量存储

### 3. API安全
- **访问控制**: 所有API需要认证
- **频率限制**: 每分钟最多100次请求
- **请求大小限制**: 最大10MB
- **CSRF保护**: 自动验证CSRF令牌

### 4. 会话管理
- **过期时间**: 2小时（可滑动）
- **Cookie安全**: HttpOnly, Secure, SameSite=Strict
- **并发控制**: 单点登录（可选）

### 5. 安全头设置
- **X-Content-Type-Options**: nosniff
- **X-Frame-Options**: DENY
- **X-XSS-Protection**: 1; mode=block
- **Content-Security-Policy**: 严格的内容安全策略
- **Strict-Transport-Security**: 强制HTTPS

## 部署检查清单

### 生产环境部署前检查

- [ ] 设置强加密密钥（ENCRYPTION_KEY）
- [ ] 配置HTTPS证书
- [ ] 更新数据库连接字符串
- [ ] 配置防火墙规则
- [ ] 设置日志监控
- [ ] 配置备份策略
- [ ] 测试所有安全功能

### 安全测试

```bash
# 测试密码策略
curl -X POST https://your-domain.com/Account/Register \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "Email=test@example.com&Password=weak123&ConfirmPassword=weak123"

# 测试API频率限制
for i in {1..110}; do
  curl -X GET https://your-domain.com/api/accounts \
    -H "Authorization: Bearer your-token"
done

# 测试CSRF保护
curl -X POST https://your-domain.com/api/accounts \
  -H "Content-Type: application/json" \
  -d '{"name":"test"}' \
  # 应该返回403错误
```

## 监控和日志

### 安全事件日志

系统会记录以下安全事件：
- 登录成功/失败
- 密码验证失败
- API访问频率超限
- CSRF令牌验证失败
- 异常访问模式

### 监控指标

建议监控以下指标：
- 登录成功率
- API响应时间
- 错误率统计
- 安全事件数量
- 系统资源使用率

## 故障排除

### 常见问题

1. **加密密钥错误**
   ```
   错误: 加密密钥未配置
   解决: 设置ENCRYPTION_KEY环境变量
   ```

2. **CSRF令牌验证失败**
   ```
   错误: CSRF令牌验证失败
   解决: 确保前端正确发送X-CSRF-TOKEN头
   ```

3. **密码策略过于严格**
   ```
   错误: 密码验证失败
   解决: 使用符合策略的强密码
   ```

### 调试模式

在开发环境中，可以设置以下配置来获取更多调试信息：

```json
{
  "Logging": {
    "LogLevel": {
      "WebUI.Middleware": "Debug",
      "WebUI.Services.PasswordValidatorService": "Debug",
      "WebUI.Services.SecureEncryptionService": "Debug"
    }
  }
}
```

## 更新和维护

### 定期维护任务

1. **密钥轮换**: 每6个月更换加密密钥
2. **安全更新**: 及时更新依赖包
3. **日志审查**: 定期检查安全日志
4. **渗透测试**: 每年进行安全测试

### 安全更新

当发现安全漏洞时：
1. 立即评估影响范围
2. 应用安全补丁
3. 更新安全配置
4. 通知相关用户
5. 更新文档

## 联系和支持

如有安全问题或需要技术支持，请联系：
- 安全邮箱: security@your-domain.com
- 技术支持: support@your-domain.com
- 紧急联系: +1-xxx-xxx-xxxx
