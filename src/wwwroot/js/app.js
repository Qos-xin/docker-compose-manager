// 全局变量
let token = localStorage.getItem('token');
let currentSearchParams = {};

// DOM 元素
const loginPage = document.getElementById('login-page');
const dashboardPage = document.getElementById('dashboard-page');
const loginBtn = document.getElementById('login-btn');
const logoutBtn = document.getElementById('logout-btn');
const loginError = document.getElementById('login-error');
const username = document.getElementById('username');
const password = document.getElementById('password');
const searchForm = document.getElementById('search-form');
const clearSearchBtn = document.getElementById('clear-search-btn');
const servicesList = document.getElementById('services-list');
const servicesTitle = document.getElementById('services-title');
const alertContainer = document.getElementById('alert-container');

// 初始化应用
function initApp() {
    if (token) {
        showDashboard();
        loadServices();
    } else {
        showLogin();
    }
    
    // 添加事件监听器
    loginBtn.addEventListener('click', handleLogin);
    logoutBtn.addEventListener('click', handleLogout);
    searchForm.addEventListener('submit', handleSearch);
    clearSearchBtn.addEventListener('click', clearSearch);
    
    // 允许按回车键登录
    password.addEventListener('keypress', function(e) {
        if (e.key === 'Enter') {
            handleLogin();
        }
    });
}

// 显示登录页面
function showLogin() {
    loginPage.style.display = 'flex';
    dashboardPage.style.display = 'none';
}

// 显示控制面板页面
function showDashboard() {
    loginPage.style.display = 'none';
    dashboardPage.style.display = 'flex';
}

// 处理登录
async function handleLogin() {
    const usernameValue = username.value.trim();
    const passwordValue = password.value.trim();
    
    if (!usernameValue || !passwordValue) {
        showError(loginError, '用户名和密码不能为空');
        return;
    }
    
    // 显示加载状态
    loginBtn.innerHTML = '<span class="spinner"></span> 登录中...';
    loginBtn.disabled = true;
    
    try {
        const response = await fetch('/api/auth/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                username: usernameValue,
                password: passwordValue
            })
        });
        
        const data = await response.json();
        
        if (response.ok && data.success) {
            // 登录成功
            token = data.token;
            localStorage.setItem('token', token);
            
            // 添加成功动画
            loginBtn.innerHTML = '<span class="success-icon">✓</span> 登录成功';
            
            // 延迟显示控制面板，以便用户看到成功消息
            setTimeout(() => {
                showDashboard();
                loadServices();
                // 重置按钮状态
                loginBtn.innerHTML = '登录';
                loginBtn.disabled = false;
            }, 1000);
        } else {
            // 登录失败
            showError(loginError, data.error || '登录失败，请检查用户名和密码');
            loginBtn.innerHTML = '登录';
            loginBtn.disabled = false;
        }
    } catch (error) {
        showError(loginError, '网络错误，请稍后重试');
        loginBtn.innerHTML = '登录';
        loginBtn.disabled = false;
    }
}

// 处理登出
function handleLogout() {
    token = null;
    localStorage.removeItem('token');
    showLogin();
}

// 处理搜索表单提交
function handleSearch(event) {
    event.preventDefault();
    
    const formData = new FormData(searchForm);
    const searchParams = {};
    
    for (const [key, value] of formData.entries()) {
        if (value.trim()) {
            searchParams[key] = value.trim();
        }
    }
    
    // 如果有 nameSearch，将其同时用于 dirSearch
    if (searchParams.nameSearch) {
        searchParams.dirSearch = searchParams.nameSearch;
    }
    
    currentSearchParams = searchParams;
    loadServices(searchParams);
}

// 清除搜索
function clearSearch() {
    searchForm.reset();
    currentSearchParams = {};
    loadServices();
}

// 加载服务列表
async function loadServices(searchParams = {}) {
    if (!token) return;
    
    try {
        // 构建查询字符串
        const queryParams = new URLSearchParams();
        for (const [key, value] of Object.entries(searchParams)) {
            queryParams.append(key, value);
        }
        
        const response = await fetch(`/api/dockercompose/services?${queryParams.toString()}`, {
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });
        
        if (response.status === 401) {
            // Token 过期或无效
            token = null;
            localStorage.removeItem('token');
            showLogin();
            showError(loginError, '会话已过期，请重新登录');
            return;
        }
        
        const data = await response.json();
        
        if (response.ok) {
            renderServices(data.services, data.totalServices, searchParams);
        } else {
            showAlert('加载服务列表失败', 'error');
        }
    } catch (error) {
        showAlert('网络错误，请稍后重试', 'error');
        console.error('Load services error:', error);
    }
}

// 渲染服务列表
function renderServices(services, totalServices, searchParams) {
    servicesList.innerHTML = '';
    
    // 更新标题
    let titleText = '服务列表';
    if (Object.keys(searchParams).length > 0) {
        titleText += ` <span class="search-results">- 搜索结果 (${totalServices}个服务)`;
        
        if (searchParams.nameSearch) {
            titleText += ` | 项目: "${searchParams.nameSearch}"`;
        }
        if (searchParams.imageSearch) {
            titleText += ` | 镜像: "${searchParams.imageSearch}"`;
        }
        if (searchParams.versionSearch) {
            titleText += ` | 版本: "${searchParams.versionSearch}"`;
        }
        if (searchParams.statusSearch) {
            titleText += ` | 状态: "${searchParams.statusSearch}"`;
        }
        
        titleText += '</span>';
    }
    
    servicesTitle.innerHTML = titleText;
    
    if (Object.keys(services).length === 0) {
        if (Object.keys(searchParams).length > 0) {
            servicesList.innerHTML = '<p>没有找到匹配的服务</p>';
        } else {
            servicesList.innerHTML = '<p>没有找到服务目录或无法读取 docker-compose.yml 文件</p>';
        }
        return;
    }
    
    // 渲染每个服务目录
    for (const [serviceDir, serviceData] of Object.entries(services)) {
        const serviceGroup = document.createElement('div');
        serviceGroup.className = 'service-group';
        
        serviceGroup.innerHTML = `
            <h3>${serviceDir}</h3>
            <p class="service-path">路径: ${serviceData.path}</p>
        `;
        
        if (Object.keys(serviceData.services).length === 0) {
            serviceGroup.innerHTML += '<p>此服务目录中没有找到服务</p>';
        } else {
            const table = document.createElement('table');
            table.className = 'services-table';
            
            table.innerHTML = `
                <thead>
                    <tr>
                        <th>服务名称</th>
                        <th>镜像</th>
                        <th>当前版本</th>
                        <th>状态</th>
                        <th>操作</th>
                    </tr>
                </thead>
                <tbody>
                </tbody>
            `;
            
            const tbody = table.querySelector('tbody');
            
            // 渲染每个服务
            for (const [serviceName, serviceInfo] of Object.entries(serviceData.services)) {
                const tr = document.createElement('tr');
                
                tr.innerHTML = `
                    <td data-label="服务名称">${serviceName}</td>
                    <td data-label="镜像">${serviceInfo.image}</td>
                    <td data-label="当前版本">${serviceInfo.version}</td>
                    <td data-label="状态">
                        <span 
                            class="status-badge status-${getStatusClass(serviceInfo.status)}" 
                            data-service-dir="${serviceDir}" 
                            data-service-name="${serviceName}">
                            ${serviceInfo.status}
                        </span>
                        <button 
                            class="btn btn-refresh" 
                            onclick="refreshStatus('${serviceDir}', '${serviceName}', this.previousElementSibling)">
                            刷新
                        </button>
                    </td>
                    <td data-label="操作" class="actions">
                        <form onsubmit="event.preventDefault(); updateVersion('${serviceDir}', '${serviceName}', this.querySelector('input').value);" class="inline-form">
                            <input type="text" placeholder="新版本号" required>
                            <button type="submit" class="btn btn-primary">更新</button>
                        </form>
                        
                        <button onclick="restartService('${serviceDir}', '${serviceName}')" class="btn btn-warning">重启</button>
                        <button onclick="stopService('${serviceDir}', '${serviceName}')" class="btn btn-danger">停止</button>
                        <button onclick="startService('${serviceDir}', '${serviceName}')" class="btn btn-success">启动</button>
                    </td>
                `;
                
                tbody.appendChild(tr);
            }
            
            serviceGroup.appendChild(table);
        }
        
        servicesList.appendChild(serviceGroup);
    }
    
    // 初始化所有状态标签的样式
    document.querySelectorAll('.status-badge').forEach(function(badge) {
        const status = badge.textContent.trim();
        badge.className = `status-badge status-${getStatusClass(status)}`;
        
        // 如果状态是"待查询"，立即刷新
        if (status === "待查询") {
            const serviceDir = badge.getAttribute('data-service-dir');
            const serviceName = badge.getAttribute('data-service-name');
            refreshStatus(serviceDir, serviceName, badge);
        }
    });
}

// 显示错误信息
function showError(element, message) {
    element.textContent = message;
    element.style.display = 'block';
    
    // 添加抖动动画
    element.classList.add('shake');
    setTimeout(() => {
        element.classList.remove('shake');
    }, 500);
}

// 显示提示信息
function showAlert(message, type = 'success') {
    const alert = document.createElement('div');
    alert.className = `alert alert-${type}`;
    alert.textContent = message;
    
    alertContainer.appendChild(alert);
    
    // 5秒后自动移除
    setTimeout(() => {
        alert.remove();
    }, 5000);
}

// 获取状态样式类
function getStatusClass(status) {
    switch(status) {
        case '运行中': return 'running';
        case '已停止': return 'stopped';
        case '未配置': return 'unknown';
        case '错误': return 'error';
        case '刷新中...': return 'refreshing';
        case '待查询': return '待查询';
        default: return 'unknown';
    }
}

// 刷新服务状态
async function refreshStatus(serviceDir, serviceName, statusElement) {
    if (!token) return;
    
    statusElement.textContent = "刷新中...";
    statusElement.className = 'status-badge status-refreshing';
    
    try {
        const response = await fetch('/api/dockercompose/refresh-status', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                serviceDir: serviceDir,
                serviceName: serviceName
            })
        });
        
        if (response.status === 401) {
            // Token 过期或无效
            token = null;
            localStorage.removeItem('token');
            showLogin();
            showError(loginError, '会话已过期，请重新登录');
            return;
        }
        
        const status = await response.text();
        
        if (response.ok) {
            statusElement.textContent = status;
            statusElement.className = `status-badge status-${getStatusClass(status)}`;
        } else {
            statusElement.textContent = "刷新失败";
            statusElement.className = 'status-badge status-error';
        }
    } catch (error) {
        statusElement.textContent = "刷新失败";
        statusElement.className = 'status-badge status-error';
        console.error('Refresh status error:', error);
    }
}

// 更新服务版本
async function updateVersion(serviceDir, serviceName, newVersion) {
    if (!token) return;
    
    try {
        const response = await fetch('/api/dockercompose/update-version', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                serviceDir: serviceDir,
                serviceName: serviceName,
                newVersion: newVersion
            })
        });
        
        if (response.status === 401) {
            // Token 过期或无效
            token = null;
            localStorage.removeItem('token');
            showLogin();
            showError(loginError, '会话已过期，请重新登录');
            return;
        }
        
        const data = await response.json();
        
        if (response.ok) {
            showAlert(data.message, 'success');
            // 重新加载服务列表
            loadServices(currentSearchParams);
        } else {
            showAlert(data.message, 'error');
        }
    } catch (error) {
        showAlert('网络错误，请稍后重试', 'error');
        console.error('Update version error:', error);
    }
}

// 重启服务
async function restartService(serviceDir, serviceName) {
    if (!token) return;
    
    try {
        const response = await fetch('/api/dockercompose/restart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                serviceDir: serviceDir,
                serviceName: serviceName
            })
        });
        
        if (response.status === 401) {
            // Token 过期或无效
            token = null;
            localStorage.removeItem('token');
            showLogin();
            showError(loginError, '会话已过期，请重新登录');
            return;
        }
        
        const data = await response.json();
        
        if (response.ok) {
            showAlert(data.message, 'success');
            // 重新加载服务列表
            setTimeout(() => {
                loadServices(currentSearchParams);
            }, 1000);
        } else {
            showAlert(data.message, 'error');
        }
    } catch (error) {
        showAlert('网络错误，请稍后重试', 'error');
        console.error('Restart service error:', error);
    }
}

// 停止服务
async function stopService(serviceDir, serviceName) {
    if (!token) return;
    
    try {
        const response = await fetch('/api/dockercompose/stop', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                serviceDir: serviceDir,
                serviceName: serviceName
            })
        });
        
        if (response.status === 401) {
            // Token 过期或无效
            token = null;
            localStorage.removeItem('token');
            showLogin();
            showError(loginError, '会话已过期，请重新登录');
            return;
        }
        
        const data = await response.json();
        
        if (response.ok) {
            showAlert(data.message, 'success');
            // 重新加载服务列表
            setTimeout(() => {
                loadServices(currentSearchParams);
            }, 1000);
        } else {
            showAlert(data.message, 'error');
        }
    } catch (error) {
        showAlert('网络错误，请稍后重试', 'error');
        console.error('Stop service error:', error);
    }
}

// 启动服务
async function startService(serviceDir, serviceName) {
    if (!token) return;
    
    try {
        const response = await fetch('/api/dockercompose/start', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                serviceDir: serviceDir,
                serviceName: serviceName
            })
        });
        
        if (response.status === 401) {
            // Token 过期或无效
            token = null;
            localStorage.removeItem('token');
            showLogin();
            showError(loginError, '会话已过期，请重新登录');
            return;
        }
        
        const data = await response.json();
        
        if (response.ok) {
            showAlert(data.message, 'success');
            // 重新加载服务列表
            setTimeout(() => {
                loadServices(currentSearchParams);
            }, 1000);
        } else {
            showAlert(data.message, 'error');
        }
    } catch (error) {
        showAlert('网络错误，请稍后重试', 'error');
        console.error('Start service error:', error);
    }
}

// 页面加载完成后初始化应用
document.addEventListener('DOMContentLoaded', initApp); 