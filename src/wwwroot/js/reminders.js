// 养号提醒JavaScript文件

// 页面加载完成后初始化
document.addEventListener('DOMContentLoaded', function() {
    initializeReminders();
});

// 初始化提醒功能
function initializeReminders() {
    // 只在需要时才初始化选中计数功能
    const hasSelectionElements = document.getElementById('selectedCount') || 
                                document.getElementById('selectAll') || 
                                document.querySelector('.reminder-checkbox');
    
    if (hasSelectionElements) {
        updateSelectedCount();
    }
    console.log('养号提醒页面已初始化');
}

// 筛选提醒
function filterReminders() {
    const reminderTypeFilter = document.getElementById('reminderTypeFilter');
    const categoryFilter = document.getElementById('categoryFilter');
    const searchKeyword = document.getElementById('searchKeyword');
    
    if (!reminderTypeFilter || !categoryFilter || !searchKeyword) {
        console.warn('筛选器元素不存在');
        return;
    }
    
    const filterValue = reminderTypeFilter.value;
    const categoryValue = categoryFilter.value;
    const keyword = searchKeyword.value.toLowerCase();
    
    const rows = document.querySelectorAll('#remindersTable tbody tr');
    
    rows.forEach(row => {
        let show = true;
        
        // 状态筛选
        if (filterValue && row.dataset.status !== filterValue) {
            show = false;
        }
        
        // 分类筛选
        if (categoryValue && row.dataset.category !== categoryValue) {
            show = false;
        }
        
        // 关键词搜索
        if (keyword) {
            const accountName = row.querySelector('td:nth-child(2)')?.textContent?.toLowerCase() || '';
            const username = row.querySelector('td:nth-child(3)')?.textContent?.toLowerCase() || '';
            if (!accountName.includes(keyword) && !username.includes(keyword)) {
                show = false;
            }
        }
        
        row.style.display = show ? '' : 'none';
    });
}

// 切换全选
function toggleSelectAll() {
    const selectAll = document.getElementById('selectAll');
    const checkboxes = document.querySelectorAll('.reminder-checkbox');
    
    if (!selectAll || checkboxes.length === 0) {
        console.warn('全选元素不存在');
        return;
    }
    
    checkboxes.forEach(checkbox => {
        checkbox.checked = selectAll.checked;
    });
    
    updateSelectedCount();
}

// 更新选中数量
function updateSelectedCount() {
    const checkboxes = document.querySelectorAll('.reminder-checkbox:checked');
    const selectedCount = document.getElementById('selectedCount');
    const batchRecordBtn = document.getElementById('batchRecordBtn');
    const batchReminderBtn = document.getElementById('batchReminderBtn');
    const batchExportBtn = document.getElementById('batchExportBtn');
    
    // 检查元素是否存在
    if (selectedCount) {
        selectedCount.textContent = checkboxes.length;
    }
    
    // 检查按钮是否存在
    if (batchRecordBtn) {
        batchRecordBtn.disabled = checkboxes.length === 0;
    }
    if (batchReminderBtn) {
        batchReminderBtn.disabled = checkboxes.length === 0;
    }
    if (batchExportBtn) {
        batchExportBtn.disabled = checkboxes.length === 0;
    }
}

// 监听复选框变化
document.addEventListener('change', function(e) {
    if (e.target.classList.contains('reminder-checkbox')) {
        updateSelectedCount();
    }
});

// 刷新提醒
function refreshReminders() {
    location.reload();
}

// 导出提醒
function exportReminders() {
    // 实现导出功能
    showToast('导出功能开发中...', 'info');
}

// 发送测试提醒
function sendTestReminder() {
    showToast('测试提醒已发送', 'success');
}

// 记录访问
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

// 编辑提醒设置
function editReminder(accountId) {
    window.location.href = `/Accounts/Edit/${accountId}`;
}

// 查看历史
function viewHistory(accountId) {
    window.location.href = `/Accounts/History/${accountId}`;
}

// 批量记录访问
async function batchRecordVisit() {
    const selectedIds = getSelectedIds();
    if (selectedIds.length === 0) {
        showToast('请选择要操作的账号', 'warning');
        return;
    }

    if (!confirm(`确定要记录 ${selectedIds.length} 个账号的访问吗？`)) {
        return;
    }

    try {
        let successCount = 0;
        for (const id of selectedIds) {
            try {
                const response = await fetch(`/api/accounts/${id}/visit`, {
                    method: 'POST'
                });
                if (response.ok) {
                    successCount++;
                }
            } catch (error) {
                console.error(`记录账号 ${id} 访问失败:`, error);
            }
        }

        showToast(`成功记录 ${successCount} 个账号的访问`, 'success');
        location.reload();
    } catch (error) {
        console.error('批量记录访问失败:', error);
        showToast('批量记录访问失败', 'error');
    }
}

// 批量设置提醒
async function batchSetReminder() {
    const selectedIds = getSelectedIds();
    if (selectedIds.length === 0) {
        showToast('请选择要操作的账号', 'warning');
        return;
    }

    const reminderDays = prompt('请输入提醒天数（天）:', '7');
    if (!reminderDays || isNaN(reminderDays)) {
        showToast('请输入有效的天数', 'warning');
        return;
    }

    try {
        let successCount = 0;
        for (const id of selectedIds) {
            try {
                const response = await fetch(`/api/accounts/${id}/reminder`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ reminderDays: parseInt(reminderDays) })
                });
                if (response.ok) {
                    successCount++;
                }
            } catch (error) {
                console.error(`设置账号 ${id} 提醒失败:`, error);
            }
        }

        showToast(`成功设置 ${successCount} 个账号的提醒`, 'success');
        location.reload();
    } catch (error) {
        console.error('批量设置提醒失败:', error);
        showToast('批量设置提醒失败', 'error');
    }
}

// 批量导出
function batchExport() {
    const selectedIds = getSelectedIds();
    if (selectedIds.length === 0) {
        showToast('请选择要导出的账号', 'warning');
        return;
    }

    // 实现导出功能
    showToast(`导出 ${selectedIds.length} 个账号信息`, 'info');
}

// 获取选中的ID列表
function getSelectedIds() {
    const checkboxes = document.querySelectorAll('.reminder-checkbox:checked');
    return Array.from(checkboxes).map(cb => cb.value);
}

// 显示提示消息
function showToast(message, type = 'info') {
    // 简单的提示实现
    const alertClass = type === 'success' ? 'alert-success' : 
                      type === 'error' ? 'alert-danger' : 
                      type === 'warning' ? 'alert-warning' : 'alert-info';
    
    const toast = document.createElement('div');
    toast.className = `alert ${alertClass} alert-dismissible fade show position-fixed`;
    toast.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
    toast.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    document.body.appendChild(toast);
    
    // 自动移除
    setTimeout(() => {
        if (toast.parentNode) {
            toast.parentNode.removeChild(toast);
        }
    }, 5000);
}
