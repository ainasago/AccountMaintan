// 网站管理JavaScript

let currentWebsiteId = null;
let statusCheckInterval = null;
let csrfToken = null;

// 页面加载完成后初始化
$(document).ready(function() {
    initializeWebsiteManagement();
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

// 初始化网站管理
function initializeWebsiteManagement() {
    // 设置状态徽章样式
    updateStatusBadges();
    
    // 初始化搜索和筛选
    setupSearchAndFilter();
    
    // 加载分类和标签选项
    loadCategoryOptions();
    loadTagOptions();
}

// 设置事件监听器
function setupEventListeners() {
    // 创建网站表单提交
    $('#createWebsiteForm').on('submit', function(e) {
        e.preventDefault();
        createWebsite();
    });

    // 搜索输入框
    $('#searchInput').on('input', filterWebsites);
    
    // 状态筛选
    $('#statusFilter').on('change', filterWebsites);
    
    // 服务器筛选
    $('#serverFilter').on('change', filterWebsites);
    
    // HTTPS复选框变化（创建表单）
    $('#useHttps').on('change', function() {
        const portInput = $('#websitePort');
        if ($(this).is(':checked')) {
            portInput.val('443');
        } else {
            portInput.val('80');
        }
    });

    // HTTPS复选框变化（编辑表单）
    $('#editWebsiteUseHttps').on('change', function() {
        const portInput = $('#editWebsitePort');
        if ($(this).is(':checked')) {
            portInput.val('443');
        } else {
            portInput.val('80');
        }
    });

    // 编辑表单保存按钮
    $('#editWebsiteModal .btn-primary').on('click', saveWebsiteEdit);
}

// 设置搜索和筛选
function setupSearchAndFilter() {
    // 实时搜索
    $('#searchInput').on('input', function() {
        filterWebsites();
    });
    
    // 筛选器变化
    $('#statusFilter, #serverFilter, #categoryFilter, #tagFilter').on('change', function() {
        filterWebsites();
    });
}

// 筛选网站
function filterWebsites() {
    const searchTerm = $('#searchInput').val().toLowerCase();
    const statusFilter = $('#statusFilter').val();
    const serverFilter = $('#serverFilter').val();
    const categoryFilter = $('#categoryFilter').val();
    const tagFilter = $('#tagFilter').val();
    
    $('#websitesTable tbody tr').each(function() {
        const $row = $(this);
        const websiteName = $row.find('td:first strong').text().toLowerCase();
        const domain = $row.find('td:nth-child(2)').text().toLowerCase();
        const status = $row.data('status');
        const serverId = $row.data('server-id');
        const category = $row.data('category') || '';
        const tags = $row.data('tags') || '';
        
        let showRow = true;
        
        // 搜索筛选
        if (searchTerm && !websiteName.includes(searchTerm) && !domain.includes(searchTerm)) {
            showRow = false;
        }
        
        // 状态筛选
        if (statusFilter && status !== statusFilter) {
            showRow = false;
        }
        
        // 服务器筛选
        if (serverFilter && serverId !== serverFilter) {
            showRow = false;
        }
        
        // 分类筛选
        if (categoryFilter && category.toLowerCase() !== categoryFilter.toLowerCase()) {
            showRow = false;
        }
        
        // 标签筛选
        if (tagFilter) {
            const websiteTags = tags.toLowerCase().split(',').map(tag => tag.trim());
            if (!websiteTags.includes(tagFilter.toLowerCase())) {
                showRow = false;
            }
        }
        
        $row.toggle(showRow);
    });
}

// 清除筛选
function clearFilters() {
    $('#searchInput').val('');
    $('#statusFilter').val('');
    $('#serverFilter').val('');
    $('#categoryFilter').val('');
    $('#tagFilter').val('');
    filterWebsites();
}

// 加载分类选项
async function loadCategoryOptions() {
    try {
        const response = await fetch('/api/websites/categories');
        if (response.ok) {
            const categories = await response.json();
            const categorySelect = $('#categoryFilter');
            categorySelect.empty().append('<option value="">所有分类</option>');
            categories.forEach(category => {
                categorySelect.append(`<option value="${category}">${category}</option>`);
            });
        }
    } catch (error) {
        console.error('加载分类选项失败:', error);
    }
}

// 加载标签选项
async function loadTagOptions() {
    try {
        const response = await fetch('/api/websites/tags');
        if (response.ok) {
            const tags = await response.json();
            const tagSelect = $('#tagFilter');
            tagSelect.empty().append('<option value="">所有标签</option>');
            tags.forEach(tag => {
                tagSelect.append(`<option value="${tag}">${tag}</option>`);
            });
        }
    } catch (error) {
        console.error('加载标签选项失败:', error);
    }
}

// 创建网站
function createWebsite() {
    // 验证必填字段
    if (!$('#NewWebsite_Name').val() || !$('#NewWebsite_Domain').val() || !$('#NewWebsite_ServerId').val()) {
        showError('请填写所有必填字段');
        return;
    }
    
    // 使用表单提交而不是AJAX
    const form = document.getElementById('createWebsiteForm');
    
    // 直接提交表单，ASP.NET Core会自动处理模型绑定
    form.submit();
}

// 检查网站状态
async function checkStatus(websiteId) {
    try {
        showLoading('检查状态中...');
        
        const response = await fetch(`/api/websites/${websiteId}/status`);
        if (response.ok) {
            const status = await response.text();
            updateWebsiteStatus(websiteId, status);
            showSuccess('状态检查完成');
        } else {
            showError('检查状态失败');
        }
    } catch (error) {
        showError('检查状态失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 更新网站状态显示
function updateWebsiteStatus(websiteId, status) {
    const $badge = $(`#status-${websiteId}`);
    $badge.text(status);
    $badge.removeClass('bg-success bg-danger bg-warning bg-info bg-secondary');
    
    switch (status.toLowerCase()) {
        case 'running':
            $badge.addClass('bg-success');
            break;
        case 'stopped':
        case 'exited':
            $badge.addClass('bg-danger');
            break;
        case 'starting':
            $badge.addClass('bg-warning');
            break;
        case 'error':
            $badge.addClass('bg-danger');
            break;
        default:
            $badge.addClass('bg-secondary');
            break;
    }
    
    // 更新表格行的数据属性
    $(`tr[data-website-id="${websiteId}"]`).data('status', status);
}

// 重启网站
async function restartWebsite(websiteId) {
    if (!confirm('确定要重启这个网站吗？')) {
        return;
    }
    
    try {
        showLoading('重启网站中...');
        
        const token = await getCsrfToken();
        if (!token) {
            showError('CSRF令牌缺失，请刷新页面重试');
            return;
        }
        
        const response = await fetch(`/api/websites/${websiteId}/restart`, {
            method: 'POST',
            headers: {
                'X-CSRF-TOKEN': token,
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });
        
        if (response.ok) {
            showSuccess('网站重启成功');
            // 延迟检查状态
            setTimeout(() => checkStatus(websiteId), 2000);
        } else {
            showError('重启网站失败');
        }
    } catch (error) {
        showError('重启网站失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 查看日志
function viewLogs(websiteId) {
    currentWebsiteId = websiteId;
    $('#logsModal').modal('show');
    refreshLogs();
}

// 刷新日志
async function refreshLogs() {
    if (!currentWebsiteId) return;
    
    try {
        const lines = $('#logLines').val();
        showLoading('加载日志中...');
        
        const response = await fetch(`/api/websites/${currentWebsiteId}/logs?lines=${lines}`);
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

// 编辑网站
async function editWebsite(websiteId) {
    console.log('编辑网站函数被调用，网站ID:', websiteId);
    try {
        showLoading('加载网站信息...');
        
        // 获取网站信息
        const response = await fetch(`/api/websites/${websiteId}`);
        if (!response.ok) {
            throw new Error('获取网站信息失败');
        }
        
        const website = await response.json();
        console.log('获取到的网站信息:', website);
        
        // 填充表单
        $('#editWebsiteId').val(website.id);
        $('#editWebsiteName').val(website.name);
        $('#editWebsiteDomain').val(website.domain);
        $('#editWebsitePort').val(website.port);
        $('#editWebsiteUseHttps').prop('checked', website.useHttps);
        $('#editWebsiteServerId').val(website.serverId);
        $('#editWebsiteWebPath').val(website.webPath);
        $('#editWebsiteSupervisorProcessName').val(website.supervisorProcessName);
        $('#editWebsiteDescription').val(website.description);
        $('#editWebsiteNotes').val(website.notes);
        $('#editWebsiteTags').val(website.tags);
        $('#editWebsiteCategory').val(website.category);
        $('#editWebsiteIsActive').prop('checked', website.isActive);
        
        // 显示模态框
        $('#editWebsiteModal').modal('show');
    } catch (error) {
        console.error('编辑网站时发生错误:', error);
        showError('加载网站信息失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 保存网站编辑
async function saveWebsiteEdit() {
    try {
        const websiteId = $('#editWebsiteId').val();
        showLoading('保存网站信息...');
        
        // 获取CSRF令牌
        const token = await getCsrfToken();
        if (!token) {
            showError('CSRF令牌缺失，请刷新页面重试');
            return;
        }
        
        // 构建网站数据
        const website = {
            id: websiteId,
            name: $('#editWebsiteName').val(),
            domain: $('#editWebsiteDomain').val(),
            port: parseInt($('#editWebsitePort').val()),
            useHttps: $('#editWebsiteUseHttps').is(':checked'),
            serverId: $('#editWebsiteServerId').val(),
            webPath: $('#editWebsiteWebPath').val(),
            supervisorProcessName: $('#editWebsiteSupervisorProcessName').val(),
            description: $('#editWebsiteDescription').val(),
            notes: $('#editWebsiteNotes').val(),
            tags: $('#editWebsiteTags').val(),
            category: $('#editWebsiteCategory').val(),
            isActive: $('#editWebsiteIsActive').is(':checked')
        };
        
        // 发送更新请求
        const response = await fetch(`/api/websites/${websiteId}`, {
            method: 'PUT',
            headers: {
                'X-CSRF-TOKEN': token,
                'Content-Type': 'application/json'
            },
            credentials: 'include',
            body: JSON.stringify(website)
        });
        
        if (response.ok) {
            $('#editWebsiteModal').modal('hide');
            showSuccess('网站更新成功');
            // 延迟刷新页面，避免重复提示
            setTimeout(() => {
                location.reload();
            }, 1000);
        } else {
            showError('更新网站失败');
        }
    } catch (error) {
        showError('更新网站失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 删除网站
async function deleteWebsite(websiteId) {
    if (!confirm('确定要删除这个网站吗？此操作不可恢复！')) {
        return;
    }
    
    try {
        showLoading('删除网站中...');
        
        const token = await getCsrfToken();
        if (!token) {
            showError('CSRF令牌缺失，请刷新页面重试');
            return;
        }
        
        const response = await fetch(`/api/websites/${websiteId}`, {
            method: 'DELETE',
            headers: {
                'X-CSRF-TOKEN': token,
                'Content-Type': 'application/json'
            },
            credentials: 'include'
        });
        
        if (response.ok) {
            showSuccess('网站删除成功');
            location.reload();
        } else {
            showError('删除网站失败');
        }
    } catch (error) {
        showError('删除网站失败: ' + error.message);
    } finally {
        hideLoading();
    }
}

// 批量检查状态
async function batchCheckStatus() {
    try {
        showLoading('批量检查状态中...');
        
        const response = await fetch('/api/websites/batch-status');
        if (response.ok) {
            const statusDict = await response.json();
            
            // 更新所有网站状态
            for (const [websiteId, status] of Object.entries(statusDict)) {
                updateWebsiteStatus(websiteId, status);
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
            case 'running':
                $badge.addClass('bg-success');
                break;
            case 'stopped':
            case 'exited':
                $badge.addClass('bg-danger');
                break;
            case 'starting':
                $badge.addClass('bg-warning');
                break;
            case 'error':
                $badge.addClass('bg-danger');
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
