// 仪表板JavaScript文件

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeDashboard();
});

// 初始化仪表板
function initializeDashboard() {
    // 可以在这里添加一些初始化逻辑
    console.log('仪表板已初始化');
}

// 记录账号访问
async function recordVisit(accountId) {
    try {
        const response = await fetch(`/api/accounts/${accountId}/visit`, {
            method: 'POST'
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
