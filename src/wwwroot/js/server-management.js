// 服务器管理JavaScript

let currentServerId = null;
let statusCheckInterval = null;
let resourceChart = null;
let csrfToken = null;

// 页面加载完成后初始化
$(document).ready(function() {
    initializeServerManagement();
    setupEventListeners();
    startStatusMonitoring();
    initializeCsrfToken();
});

// 初始化CSRF令牌
async function initializeCsrfToken() {
    try {
        console.log('正在获取CSRF令牌...');
        const response = await fetch('/api/admin/csrf-token', {
            method: 'GET',
            credentials: 'include'
        });
        
        console.log('CSRF令牌响应状态:', response.status);
        
        if (response.ok) {
            const data = await response.json();
            console.log('CSRF令牌响应数据:', data);
            if (data.success) {
                csrfToken = data.token;
                console.log('CSRF令牌已获取:', csrfToken.substring(0, 8) + '...');
            } else {
                console.error('CSRF令牌获取失败:', data.message);
            }
        } else {
            console.error('CSRF令牌请求失败:', response.status, response.statusText);
        }
    } catch (error) {
        console.error('获取CSRF令牌失败:', error);
    }
}

// 获取CSRF令牌（如果不存在则重新获取）
async function getCsrfToken() {
    if (!csrfToken) {
        await initializeCsrfToken();
    }
    return csrfToken;
}

// 初始化服务器管理
function initializeServerManagement() {
    // 设置状态徽章样式
    updateStatusBadges();
    
    // 初始化搜索和筛选
    setupSearchAndFilter();
}

// 设置事件监听器
function setupEventListeners() {
    // 创建服务器表单提交
    $('#createServerForm').on('submit', function(e) {
        e.preventDefault();
        createServer();
    });

    // 搜索输入框
    $('#searchInput').on('input', filterServers);
    
    // 状态筛选
    $('#statusFilter').on('change', filterServers);
    
    // 操作系统筛选
    $('#osFilter').on('change', filterServers);
}

// 设置搜索和筛选
function setupSearchAndFilter() {
    // 实时搜索
    $('#searchInput').on('input', function() {
        filterServers();
    });
    
    // 筛选器变化
    $('#statusFilter, #osFilter').on('change', function() {
        filterServers();
    });
}

// 筛选服务器
function filterServers() {
    const searchTerm = $('#searchInput').val().toLowerCase();
    const statusFilter = $('#statusFilter').val();
    const osFilter = $('#osFilter').val();
    
    $('#serversTable tbody tr').each(function() {
        const $row = $(this);
        const serverName = $row.find('td:first strong').text().toLowerCase();
        const ipAddress = $row.find('td:nth-child(2) code').text().toLowerCase();
        const status = $row.data('status');
        const os = $row.data('os');
        
        let showRow = true;
        
        // 搜索筛选
        if (searchTerm && !serverName.includes(searchTerm) && !ipAddress.includes(searchTerm)) {
            showRow = false;
        }
        
        // 状态筛选
        if (statusFilter && status !== statusFilter) {
            showRow = false;
        }
        
        // 操作系统筛选
        if (osFilter && os !== osFilter) {
            showRow = false;
        }
        
        $row.toggle(showRow);
    });
}

// 清除筛选
function clearFilters() {
    $('#searchInput').val('');
    $('#statusFilter').val('');
    $('#osFilter').val('');
    filterServers();
}

// 创建服务器
function createServer() {
    // 验证必填字段
    if (!$('#NewServer_Name').val() || !$('#NewServer_IpAddress').val() || !$('#NewServer_SshUsername').val() || !$('#NewServer_SshPassword').val()) {
        showError('请填写所有必填字段');
        return;
    }
    
    // 使用表单提交而不是AJAX
    const form = document.getElementById('createServerForm');
    
    // 直接提交表单，ASP.NET Core会自动处理模型绑定
    form.submit();
}

// 测试连接
async function testConnection(serverId) {
    try {
        showLoading('测试连接中...');
        
        const token = await getCsrfToken();
        if (!token) {
            showError('CSRF令牌缺失，请刷新页面重试');
            return;
        }
        
        const response = await fetch(`/api/servers/${serverId}/test-connection`, {
            method: 'POST',
            headers: {
                'X-CSRF-TOKEN': token,
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });
        
        if (response.ok) {
            const isConnected = await response.json();
            updateServerStatus(serverId, isConnected ? 'Connected' : 'Failed');
            showSuccess(isConnected ? '连接成功' : '连接失败');
        } else {
            showError('测试连接失败');
        }
    } catch (error) {
        showError('测试连接失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 创建前测试连接
async function testConnectionBeforeCreate() {
    const serverData = {
        name: $('#NewServer_Name').val(),
        ipAddress: $('#NewServer_IpAddress').val(),
        sshPort: parseInt($('#NewServer_SshPort').val()) || 22,
        sshUsername: $('#NewServer_SshUsername').val(),
        sshPassword: $('#NewServer_SshPassword').val(),
        sshPrivateKeyPath: $('#NewServer_SshPrivateKeyPath').val()
    };
    
    try {
        showLoading('测试连接中...');
        
        // 这里需要创建一个临时的服务器对象进行测试
        // 由于没有服务器ID，我们直接调用SSH服务
        showSuccess('连接测试功能开发中...');
    } catch (error) {
        showError('测试连接失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 更新服务器状态显示
function updateServerStatus(serverId, status) {
    const $badge = $(`#status-${serverId}`);
    $badge.text(status);
    $badge.removeClass('bg-success bg-danger bg-warning bg-info bg-secondary');
    
    switch (status.toLowerCase()) {
        case 'connected':
            $badge.addClass('bg-success');
            break;
        case 'failed':
        case 'disconnected':
            $badge.addClass('bg-danger');
            break;
        case 'connecting':
            $badge.addClass('bg-warning');
            break;
        default:
            $badge.addClass('bg-secondary');
            break;
    }
    
    // 更新表格行的数据属性
    $(`tr[data-server-id="${serverId}"]`).data('status', status);
}

// 查看资源使用情况
async function viewResources(serverId) {
    currentServerId = serverId;
    $('#resourcesModal').modal('show');
    await loadServerResources(serverId);
}

// 加载服务器资源信息
async function loadServerResources(serverId) {
    try {
        showLoading('加载资源信息中...');
        
        const response = await fetch(`/api/servers/${serverId}/resources`);
        if (response.ok) {
            const resources = await response.json();
            updateResourceDisplay(resources);
            await loadResourceHistory(serverId);
        } else {
            showError('获取资源信息失败');
        }
    } catch (error) {
        showError('获取资源信息失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 更新资源显示
function updateResourceDisplay(resources) {
    $('#cpuUsage').text(resources.cpuUsage ? resources.cpuUsage.toFixed(1) + '%' : '--%');
    $('#memoryUsage').text(resources.memoryUsage ? resources.memoryUsage.toFixed(1) + '%' : '--%');
    $('#diskUsage').text(resources.diskUsage ? resources.diskUsage.toFixed(1) + '%' : '--%');
    $('#loadAverage').text(resources.loadAverage ? resources.loadAverage.toFixed(2) : '--');
    $('#uptime').text(formatUptime(resources.uptime));
    $('#processCount').text(resources.processCount || '--');
    $('#networkIn').text(formatBytes(resources.networkInBytes));
    $('#networkOut').text(formatBytes(resources.networkOutBytes));
}

// 加载资源历史数据
async function loadResourceHistory(serverId) {
    try {
        const response = await fetch(`/api/servers/${serverId}/resources/history?hours=24`);
        if (response.ok) {
            const history = await response.json();
            createResourceChart(history);
        }
    } catch (error) {
        console.error('获取资源历史数据失败:', error);
    }
}

// 创建资源图表
function createResourceChart(history) {
    const ctx = document.getElementById('resourceChart').getContext('2d');
    
    if (resourceChart) {
        resourceChart.destroy();
    }
    
    const labels = history.map(h => new Date(h.recordTime).toLocaleTimeString());
    const cpuData = history.map(h => h.cpuUsage || 0);
    const memoryData = history.map(h => h.memoryUsage || 0);
    const diskData = history.map(h => h.diskUsage || 0);
    
    resourceChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [
                {
                    label: 'CPU使用率 (%)',
                    data: cpuData,
                    borderColor: 'rgb(75, 192, 192)',
                    backgroundColor: 'rgba(75, 192, 192, 0.2)',
                    tension: 0.1
                },
                {
                    label: '内存使用率 (%)',
                    data: memoryData,
                    borderColor: 'rgb(255, 99, 132)',
                    backgroundColor: 'rgba(255, 99, 132, 0.2)',
                    tension: 0.1
                },
                {
                    label: '磁盘使用率 (%)',
                    data: diskData,
                    borderColor: 'rgb(255, 205, 86)',
                    backgroundColor: 'rgba(255, 205, 86, 0.2)',
                    tension: 0.1
                }
            ]
        },
        options: {
            responsive: true,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100
                }
            }
        }
    });
}

// 查看进程列表
async function viewProcesses(serverId) {
    currentServerId = serverId;
    $('#processesModal').modal('show');
    await loadServerProcesses(serverId);
}

// 加载服务器进程列表
async function loadServerProcesses(serverId) {
    try {
        showLoading('加载进程列表中...');
        
        const response = await fetch(`/api/servers/${serverId}/processes`);
        if (response.ok) {
            const processes = await response.json();
            updateProcessesTable(processes);
        } else {
            showError('获取进程列表失败');
        }
    } catch (error) {
        showError('获取进程列表失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 更新进程表格
function updateProcessesTable(processes) {
    const tbody = $('#processesTableBody');
    tbody.empty();
    
    for (const [processName, processInfo] of Object.entries(processes)) {
        const row = `
            <tr>
                <td>${processName}</td>
                <td><span class="badge ${getProcessStatusClass(processInfo.status)}">${processInfo.status}</span></td>
                <td>${processInfo.processId || '--'}</td>
                <td>${processInfo.uptime || '--'}</td>
                <td>
                    <div class="btn-group" role="group">
                        <button type="button" class="btn btn-sm btn-outline-success" onclick="startProcess('${processName}')" title="启动">
                            <i class="fas fa-play"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-danger" onclick="stopProcess('${processName}')" title="停止">
                            <i class="fas fa-stop"></i>
                        </button>
                        <button type="button" class="btn btn-sm btn-outline-warning" onclick="restartProcess('${processName}')" title="重启">
                            <i class="fas fa-redo"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
        tbody.append(row);
    }
}

// 获取进程状态样式类
function getProcessStatusClass(status) {
    switch (status.toLowerCase()) {
        case 'running':
            return 'bg-success';
        case 'stopped':
        case 'exited':
            return 'bg-danger';
        case 'starting':
            return 'bg-warning';
        case 'fatal':
        case 'backoff':
            return 'bg-danger';
        default:
            return 'bg-secondary';
    }
}

// 启动进程
async function startProcess(processName) {
    if (!currentServerId) return;
    
    try {
        showLoading('启动进程中...');
        
        const token = await getCsrfToken();
        if (!token) {
            showError('CSRF令牌缺失，请刷新页面重试');
            return;
        }
        
        const response = await fetch(`/api/servers/${currentServerId}/processes/${processName}/start`, {
            method: 'POST',
            headers: {
                'X-CSRF-TOKEN': token,
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });
        
        if (response.ok) {
            showSuccess('进程启动成功');
            loadServerProcesses(currentServerId);
        } else {
            showError('进程启动失败');
        }
    } catch (error) {
        showError('进程启动失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 停止进程
async function stopProcess(processName) {
    if (!currentServerId) return;
    
    try {
        showLoading('停止进程中...');
        
        const token = await getCsrfToken();
        if (!token) {
            showError('CSRF令牌缺失，请刷新页面重试');
            return;
        }
        
        const response = await fetch(`/api/servers/${currentServerId}/processes/${processName}/stop`, {
            method: 'POST',
            headers: {
                'X-CSRF-TOKEN': token,
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });
        
        if (response.ok) {
            showSuccess('进程停止成功');
            loadServerProcesses(currentServerId);
        } else {
            showError('进程停止失败');
        }
    } catch (error) {
        showError('进程停止失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 重启进程
async function restartProcess(processName) {
    if (!currentServerId) return;
    
    try {
        showLoading('重启进程中...');
        
        const token = await getCsrfToken();
        if (!token) {
            showError('CSRF令牌缺失，请刷新页面重试');
            return;
        }
        
        const response = await fetch(`/api/servers/${currentServerId}/processes/${processName}/restart`, {
            method: 'POST',
            headers: {
                'X-CSRF-TOKEN': token,
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });
        
        if (response.ok) {
            showSuccess('进程重启成功');
            loadServerProcesses(currentServerId);
        } else {
            showError('进程重启失败');
        }
    } catch (error) {
        showError('进程重启失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 查看日志
function viewLogs(serverId) {
    currentServerId = serverId;
    $('#logsModal').modal('show');
    refreshLogs();
}

// 刷新日志
async function refreshLogs() {
    if (!currentServerId) return;
    
    try {
        const logType = $('#logType').val();
        const lines = $('#logLines').val();
        showLoading('加载日志中...');
        
        const response = await fetch(`/api/servers/${currentServerId}/logs?logType=${logType}&lines=${lines}`);
        if (response.ok) {
            const logs = await response.text();
            $('#logContent').text(logs);
        } else {
            $('#logContent').text('获取日志失败');
        }
    } catch (error) {
        $('#logContent').text('获取日志失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 编辑服务器
function editServer(serverId) {
    // TODO: 实现编辑功能
    showInfo('编辑功能开发中...');
}

// 删除服务器
async function deleteServer(serverId) {
    if (!confirm('确定要删除这个服务器吗？此操作不可恢复！')) {
        return;
    }
    
    try {
        showLoading('删除服务器中...');
        
        const token = await getCsrfToken();
        if (!token) {
            showError('CSRF令牌缺失，请刷新页面重试');
            return;
        }
        
        const response = await fetch(`/api/servers/${serverId}`, {
            method: 'DELETE',
            headers: {
                'X-CSRF-TOKEN': token,
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });
        
        if (response.ok) {
            showSuccess('服务器删除成功');
            location.reload();
        } else {
            showError('删除服务器失败');
        }
    } catch (error) {
        showError('删除服务器失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 批量检查状态
async function batchCheckStatus() {
    try {
        showLoading('批量检查状态中...');
        
        const response = await fetch('/api/servers/batch-status');
        if (response.ok) {
            const statusDict = await response.json();
            
            // 更新所有服务器状态
            for (const [serverId, isConnected] of Object.entries(statusDict)) {
                updateServerStatus(serverId, isConnected ? 'Connected' : 'Failed');
            }
            
            showSuccess('批量状态检查完成');
        } else {
            showError('批量检查状态失败');
        }
    } catch (error) {
        showError('批量检查状态失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 更新状态徽章样式
function updateStatusBadges() {
    $('.status-badge').each(function() {
        const $badge = $(this);
        const status = $badge.text().toLowerCase();
        
        $badge.removeClass('bg-success bg-danger bg-warning bg-info bg-secondary');
        
        switch (status) {
            case 'connected':
                $badge.addClass('bg-success');
                break;
            case 'failed':
            case 'disconnected':
                $badge.addClass('bg-danger');
                break;
            case 'connecting':
                $badge.addClass('bg-warning');
                break;
            default:
                $badge.addClass('bg-secondary');
                break;
        }
    });
}

// 开始状态监控
function startStatusMonitoring() {
    // 每5分钟自动检查一次状态
    statusCheckInterval = setInterval(() => {
        batchCheckStatus();
    }, 5 * 60 * 1000);
}

// 停止状态监控
function stopStatusMonitoring() {
    if (statusCheckInterval) {
        clearInterval(statusCheckInterval);
        statusCheckInterval = null;
    }
}

// 页面卸载时停止监控
$(window).on('beforeunload', function() {
    stopStatusMonitoring();
});

// 工具函数
function formatUptime(seconds) {
    if (!seconds) return '--';
    
    const days = Math.floor(seconds / 86400);
    const hours = Math.floor((seconds % 86400) / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    
    if (days > 0) {
        return `${days}天 ${hours}小时 ${minutes}分钟`;
    } else if (hours > 0) {
        return `${hours}小时 ${minutes}分钟`;
    } else {
        return `${minutes}分钟`;
    }
}

function formatBytes(bytes) {
    if (!bytes) return '--';
    
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));
    return Math.round(bytes / Math.pow(1024, i) * 100) / 100 + ' ' + sizes[i];
}

function showLoading(message) {
    // 可以在这里添加加载指示器
    console.log('Loading: ' + message);
}

function hideLoading() {
    // 隐藏加载指示器
    console.log('Loading complete');
}

function showSuccess(message) {
    // 显示成功消息
    if (typeof toastr !== 'undefined') {
        toastr.success(message);
    } else {
        alert(message);
    }
}

function showError(message) {
    // 显示错误消息
    if (typeof toastr !== 'undefined') {
        toastr.error(message);
    } else {
        alert(message);
    }
}

function showInfo(message) {
    // 显示信息消息
    if (typeof toastr !== 'undefined') {
        toastr.info(message);
    } else {
        alert(message);
    }
}
