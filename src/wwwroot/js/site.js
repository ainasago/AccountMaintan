// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 检查用户权限并显示管理菜单
$(document).ready(function() {
    // 检查用户是否已登录，只有登录后才检查管理员权限
    if (isUserLoggedIn()) {
        checkAdminPermissions();
    } else {
        // 用户未登录，隐藏所有管理菜单
        hideAdminMenus();
    }
});

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
    $('#securityMenu').show();
    $('#hangfireMenu').show();
}

// 隐藏管理员菜单
function hideAdminMenus() {
    $('#adminMenu').hide();
    $('#adminSettingsMenu').hide();
    $('#settingsMenu').hide();
    $('#securityMenu').hide();
    $('#hangfireMenu').hide();
}