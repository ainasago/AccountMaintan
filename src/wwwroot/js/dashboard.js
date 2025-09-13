// 仪表板JavaScript文件

// 全局变量
let csrfToken = null;

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeDashboard();
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

// 初始化仪表板
function initializeDashboard() {
    // 可以在这里添加一些初始化逻辑
    console.log('仪表板已初始化');
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
            headers: headers,
            credentials: 'include'
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
    const toast = document.createElement('div');
    toast.className = `toast align-items-center text-white bg-${type === 'error' ? 'danger' : type} border-0`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');

    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                ${message}
            </div>
            <button type="button" class="btn btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
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
