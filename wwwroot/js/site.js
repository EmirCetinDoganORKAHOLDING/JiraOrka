// JiraOrka - Main JavaScript

document.addEventListener('DOMContentLoaded', function () {

    // ============================
    // SIDEBAR TOGGLE (Mobile)
    // ============================
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    const mobileBtn = document.getElementById('mobileMenuBtn');
    const sidebarToggle = document.getElementById('sidebarToggle');

    function openSidebar() {
        sidebar?.classList.add('open');
        overlay?.classList.add('active');
        document.body.style.overflow = 'hidden';
    }

    function closeSidebar() {
        sidebar?.classList.remove('open');
        overlay?.classList.remove('active');
        document.body.style.overflow = '';
    }

    mobileBtn?.addEventListener('click', openSidebar);
    sidebarToggle?.addEventListener('click', closeSidebar);
    overlay?.addEventListener('click', closeSidebar);

    // ============================
    // AUTO-DISMISS ALERTS
    // ============================
    const alerts = document.querySelectorAll('.alert.alert-success, .alert.alert-info');
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            bsAlert?.close();
        }, 4000);
    });

    // ============================
    // LOGOUT FORM - Fix token
    // ============================
    const logoutForms = document.querySelectorAll('form[action*="/Account/Logout"]');
    logoutForms.forEach(form => {
        const main = document.querySelector('input[name="__RequestVerificationToken"]');
        const formToken = form.querySelector('input[name="__RequestVerificationToken"]');
        if (main && formToken) formToken.value = main.value;
    });

    // ============================
    // TABLE ROW HOVER
    // ============================
    document.querySelectorAll('table tbody tr').forEach(row => {
        row.style.cursor = 'pointer';
    });

    // ============================
    // CONFIRM DELETE BUTTONS
    // ============================
    document.querySelectorAll('[data-confirm]').forEach(btn => {
        btn.addEventListener('click', e => {
            if (!confirm(btn.dataset.confirm)) {
                e.preventDefault();
                e.stopPropagation();
            }
        });
    });

    // ============================
    // COLUMN TITLE AUTO-UPPERCASE
    // ============================
    document.querySelectorAll('input.text-uppercase').forEach(input => {
        input.addEventListener('input', function () {
            this.value = this.value.toUpperCase();
        });
    });

    // ============================
    // SMOOTH TOOLTIPS
    // ============================
    const tooltipEls = document.querySelectorAll('[data-bs-toggle="tooltip"]');
    tooltipEls.forEach(el => new bootstrap.Tooltip(el));

});

// ============================
// KANBAN HELPER
// ============================
function getAntiForgeryToken() {
    const input = document.querySelector('input[name="__RequestVerificationToken"]');
    return input ? input.value : '';
}
