// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 等待jQuery加载完成
function waitForJQuery() {
    if (typeof $ !== 'undefined') {
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
    } else {
        setTimeout(waitForJQuery, 100);
    }
}

// 启动等待jQuery
waitForJQuery();

// 初始化AdminLTE
function initializeAdminLTE() {
    // 初始化侧边栏树形菜单
    $('[data-widget="treeview"]').Treeview('init');
    
    // 不初始化AdminLTE的PushMenu，使用我们的自定义逻辑
    // $('[data-widget="pushmenu"]').PushMenu('init');
    
    // 初始化移动端菜单
    initializeMobileMenu();
}

// 初始化移动端菜单
function initializeMobileMenu() {
    const $pushMenu = $('[data-widget="pushmenu"]');
    const $sidebar = $('.main-sidebar');
    const $overlay = $('#sidebarOverlay');
    
    // 重置状态，确保初始状态正确
    resetSidebarState();
    
    // 禁用AdminLTE的默认PushMenu行为，使用我们的自定义逻辑
    $pushMenu.off('click');
    
    // 点击菜单按钮
    $pushMenu.on('click', function(e) {
        e.preventDefault();
        e.stopPropagation();
        toggleSidebar();
    });
    
    // 点击遮罩层关闭菜单
    $overlay.on('click', function() {
        closeSidebar();
    });
    
    // 点击菜单项关闭菜单（移动端）
    $sidebar.find('.nav-link').on('click', function() {
        if ($(window).width() <= 767.98) {
            closeSidebar();
        }
    });
    
    // 窗口大小改变时处理
    $(window).on('resize', function() {
        if ($(window).width() > 767.98) {
            // 桌面端，确保侧边栏正常显示
            resetSidebarState();
        }
    });
}

// 重置侧边栏状态
function resetSidebarState() {
    const $sidebar = $('.main-sidebar');
    const $overlay = $('#sidebarOverlay');
    
    // 确保所有状态都被重置
    $sidebar.removeClass('sidebar-open');
    $overlay.removeClass('show');
    $('body').removeClass('sidebar-open');
    $('body').css('overflow', '');
    
    console.log('侧边栏状态已重置');
}

// 切换侧边栏
function toggleSidebar() {
    const $sidebar = $('.main-sidebar');
    const $overlay = $('#sidebarOverlay');
    
    // 检查当前状态
    const isOpen = $sidebar.hasClass('sidebar-open') && $overlay.hasClass('show');
    
    if (isOpen) {
        closeSidebar();
    } else {
        openSidebar();
    }
}

// 打开侧边栏
function openSidebar() {
    const $sidebar = $('.main-sidebar');
    const $overlay = $('#sidebarOverlay');
    
    // 确保所有相关元素都处于正确状态
    $sidebar.addClass('sidebar-open');
    $overlay.addClass('show');
    $('body').addClass('sidebar-open');
    
    // 防止body滚动
    $('body').css('overflow', 'hidden');
    
    console.log('侧边栏已打开');
}

// 关闭侧边栏
function closeSidebar() {
    const $sidebar = $('.main-sidebar');
    const $overlay = $('#sidebarOverlay');
    
    // 确保所有相关元素都处于正确状态
    $sidebar.removeClass('sidebar-open');
    $overlay.removeClass('show');
    $('body').removeClass('sidebar-open');
    
    // 恢复body滚动
    $('body').css('overflow', '');
    
    console.log('侧边栏已关闭');
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