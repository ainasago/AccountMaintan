// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 检查用户权限并显示管理菜单
$(document).ready(function() {
    // 检查用户是否为管理员
    checkAdminPermissions();
});

function checkAdminPermissions() {
    $.ajax({
        url: '/api/admin/check-permissions',
        type: 'GET',
        success: function(response) {
            if (response.isAdmin) {
                $('#adminMenu').show();
                $('#adminSettingsMenu').show();
            }
        },
        error: function() {
            // 如果API不存在或出错，隐藏管理菜单
            $('#adminMenu').hide();
            $('#adminSettingsMenu').hide();
        }
    });
}