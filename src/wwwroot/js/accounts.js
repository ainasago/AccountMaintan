// 账号管理JavaScript文件

// 全局变量
let reminderHub;
let csrfToken = null;

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeSignalR();
    initializeEventListeners();
    initializeCsrfToken();
});

// 初始化CSRF令牌
async function initializeCsrfToken() {
    try {
        const response = await fetch('/api/admin/csrf-token', {
            method: 'GET',
            credentials: 'include'
        });
        
        if (response.ok) {
            const data = await response.json();
            if (data.success) {
                csrfToken = data.token;
                console.log('CSRF令牌已获取');
            }
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

// 初始化SignalR连接
function initializeSignalR() {
    reminderHub = new signalR.HubConnectionBuilder()
        .withUrl("/reminderHub")
        .withAutomaticReconnect()
        .build();

    reminderHub.start()
        .then(() => {
            console.log("SignalR连接已建立");
            return reminderHub.invoke("JoinReminderGroup");
        })
        .catch(err => console.error("SignalR连接失败:", err));

    // 监听提醒消息
    reminderHub.on("ReceiveReminder", function(reminders) {
        showReminderNotification(reminders);
    });
}

// 初始化事件监听器
function initializeEventListeners() {
    // 新增账号表单提交
    const addAccountForm = document.getElementById('addAccountForm');
    if (addAccountForm) {
        addAccountForm.addEventListener('submit', handleAddAccount);
    }

    // 搜索框回车事件
    const searchKeyword = document.getElementById('searchKeyword');
    if (searchKeyword) {
        searchKeyword.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                searchAccounts();
            }
        });
    }
}

// 处理新增账号
async function handleAddAccount(e) {
    e.preventDefault();
    
    const formData = new FormData(e.target);
    const accountData = {
        Name: formData.get('Name'),
        Url: formData.get('Url'),
        Username: formData.get('Username'),
        Password: formData.get('Password'),
        Category: formData.get('Category'),
        Tags: formData.get('Tags'),
        ReminderCycle: parseInt(formData.get('ReminderCycle')),
        ReminderType: formData.get('ReminderType'),
        Notes: formData.get('Notes')
    };

    try {
        const token = await getCsrfToken();
        const headers = {
            'Content-Type': 'application/json',
        };
        
        if (token) {
            headers['X-CSRF-TOKEN'] = token;
        }
        
        const response = await fetch('/api/accounts', {
            method: 'POST',
            headers: headers,
            body: JSON.stringify(accountData)
        });

        if (response.ok) {
            showToast('账号创建成功', 'success');
            bootstrap.Modal.getInstance(document.getElementById('addAccountModal')).hide();
            e.target.reset();
            // 刷新页面
            location.reload();
        } else {
            const error = await response.text();
            showToast(`创建失败: ${error}`, 'error');
        }
    } catch (error) {
        console.error('创建账号失败:', error);
        showToast('创建账号时发生错误', 'error');
    }
}

// 搜索账号
function searchAccounts() {
    const keyword = document.getElementById('searchKeyword').value;
    const category = document.getElementById('categoryFilter').value;
    const reminderFilter = document.getElementById('reminderFilter').value;

    // 构建查询参数
    const params = new URLSearchParams();
    if (keyword) params.append('keyword', keyword);
    if (category) params.append('category', category);
    if (reminderFilter) params.append('reminderFilter', reminderFilter);

    // 跳转到搜索结果页面或刷新当前页面
    window.location.href = `/Accounts/Index?${params.toString()}`;
}

// 查看账号详情
function viewAccount(accountId) {
    window.location.href = `/Accounts/Details/${accountId}`;
}

// 编辑账号
function editAccount(accountId) {
    window.location.href = `/Accounts/Edit/${accountId}`;
}

// 记录账号访问
async function recordVisit(accountId) {
    try {
        const token = await getCsrfToken();
        const headers = {};
        
        if (token) {
            headers['X-CSRF-TOKEN'] = token;
        }
        
        const response = await fetch(`/api/accounts/${accountId}/visit`, {
            method: 'POST',
            headers: headers
        });

        if (response.ok) {
            showToast('访问记录已保存', 'success');
            // 刷新页面
            location.reload();
        } else {
            showToast('记录访问失败', 'error');
        }
    } catch (error) {
        console.error('记录访问失败:', error);
        showToast('记录访问时发生错误', 'error');
    }
}

// 删除账号
async function deleteAccount(accountId) {
    if (!confirm('确定要删除这个账号吗？此操作不可恢复。')) {
        return;
    }

    try {
        const token = await getCsrfToken();
        const headers = {};
        
        if (token) {
            headers['X-CSRF-TOKEN'] = token;
        }
        
        const response = await fetch(`/api/accounts/${accountId}`, {
            method: 'DELETE',
            headers: headers
        });

        if (response.ok) {
            showToast('账号已删除', 'success');
            // 删除成功后刷新页面
            setTimeout(() => location.reload(), 1000);
        } else {
            const errorData = await response.json();
            showToast(`删除失败: ${errorData.message || '未知错误'}`, 'error');
        }
    } catch (error) {
        console.error('删除账号失败:', error);
        showToast('删除账号时发生错误', 'error');
    }
}

// 导出账号
function exportAccounts() {
    // 实现导出功能
    showToast('导出功能开发中...', 'info');
}

// 切换密码显示/隐藏
function togglePassword(inputId) {
    const input = document.getElementById(inputId);
    const button = input.nextElementSibling;
    const icon = button.querySelector('i');

    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.remove('fa-eye');
        icon.classList.add('fa-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.remove('fa-eye-slash');
        icon.classList.add('fa-eye');
    }
}

// 显示提醒通知
function showReminderNotification(reminders) {
    if (!reminders || reminders.length === 0) return;

    const notification = document.createElement('div');
    notification.className = 'alert alert-warning alert-dismissible fade show position-fixed';
    notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; max-width: 400px;';
    
    notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="fas fa-bell me-2"></i>
            <strong>养号提醒</strong>
        </div>
        <hr class="my-2">
        <div class="small">
            <p class="mb-1">以下账号需要访问：</p>
            ${reminders.map(r => `
                <div class="d-flex justify-content-between align-items-center mb-1">
                    <span>${r.name}</span>
                    <span class="badge bg-danger">${Math.round(r.daysSinceLastVisit)}天</span>
                </div>
            `).join('')}
        </div>
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;

    document.body.appendChild(notification);

    // 5秒后自动关闭
    setTimeout(() => {
        if (notification.parentNode) {
            notification.remove();
        }
    }, 5000);
}

// 显示Toast消息
function showToast(message, type = 'info') {
    // 创建Toast容器（如果不存在）
    let toastContainer = document.getElementById('toastContainer');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toastContainer';
        toastContainer.className = 'position-fixed top-0 end-0 p-3';
        toastContainer.style.cssText = 'z-index: 9999;';
        document.body.appendChild(toastContainer);
    }

    // 创建Toast元素
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : type} border-0`;
    toast.id = toastId;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
        </div>
    `;

    toastContainer.appendChild(toast);

    // 显示Toast
    const bsToast = new bootstrap.Toast(toast);
    bsToast.show();

    // 自动隐藏
    setTimeout(() => {
        if (toast.parentNode) {
            toast.remove();
        }
    }, 3000);
}
