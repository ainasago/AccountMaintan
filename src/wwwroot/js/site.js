// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 初始化AdminLTE和权限检查
$(document).ready(function() {
    // 初始化AdminLTE
    initializeAdminLTE();
    
    // 检查用户权限并显示管理菜单
    if (isUserLoggedIn()) {
        checkAdminPermissions();
    } else {
        // 用户未登录，隐藏所有管理菜单
        hideAdminMenus();
    }
});

// 初始化AdminLTE
function initializeAdminLTE() {
    // 初始化侧边栏树形菜单
    $('[data-widget="treeview"]').Treeview('init');
    
    // 初始化推送菜单
    $('[data-widget="pushmenu"]').PushMenu('init');
}

// 检查用户是否已登录
function isUserLoggedIn() {
    // 检查是否存在用户下拉菜单（只有登录用户才有）
    return $('#navbarDropdown').length > 0;
}

function checkAdminPermissions() {
    $.ajax({
        url: '/api/admin/check-permissions',
        type: 'GET',
        success: function(response) {
            if (response.isAdmin) {
                // 管理员可以看到所有菜单
                showAdminMenus();
            } else {
                // 普通用户只能看到账号管理和提醒管理
                hideAdminMenus();
            }
        },
        error: function(xhr) {
            // 如果是401错误，说明用户未登录，隐藏管理菜单
            if (xhr.status === 401) {
                hideAdminMenus();
            } else {
                // 其他错误，也隐藏管理菜单
                hideAdminMenus();
            }
        }
    });
}

// 显示管理员菜单
function showAdminMenus() {
    $('#adminMenu').show();
    $('#adminSettingsMenu').show();
    $('#settingsMenu').show();
    $('#hangfireMenu').show();
}

// 隐藏管理员菜单
function hideAdminMenus() {
    $('#adminMenu').hide();
    $('#adminSettingsMenu').hide();
    $('#settingsMenu').hide();
    $('#hangfireMenu').hide();
}

// 工具函数：显示Toast通知
function showToast(message, type = 'info') {
    // 如果存在toastr，使用toastr
    if (typeof toastr !== 'undefined') {
        toastr[type](message);
    } else {
        // 否则使用alert
        alert(message);
    }
}

// 工具函数：显示加载状态
function showLoading(element, text = '加载中...') {
    if (typeof element === 'string') {
        element = $(element);
    }
    element.prop('disabled', true).html(`<i class="fas fa-spinner fa-spin"></i> ${text}`);
}

// 工具函数：隐藏加载状态
function hideLoading(element, originalText = '确定') {
    if (typeof element === 'string') {
        element = $(element);
    }
    element.prop('disabled', false).html(originalText);
}