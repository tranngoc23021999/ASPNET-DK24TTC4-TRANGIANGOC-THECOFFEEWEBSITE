/**
 * CoffeSolution - Site JavaScript
 * Mobile sidebar toggle, dropdowns, and interactive elements
 */

// ========================================
// MOBILE SIDEBAR TOGGLE
// ========================================

document.addEventListener('DOMContentLoaded', function() {
    const sidebar = document.querySelector('.sidebar');
    const sidebarOverlay = document.getElementById('sidebarOverlay');
    const hamburgerBtn = document.getElementById('hamburgerBtn');

    // Create hamburger button dynamically
    if (!hamburgerBtn && window.innerWidth <= 991) {
        createHamburgerButton();
    }

    // Toggle sidebar on hamburger click
    document.addEventListener('click', function(e) {
        if (e.target.closest('#hamburgerBtn')) {
            toggleSidebar();
        }
    });

    // Close sidebar when clicking overlay
    if (sidebarOverlay) {
        sidebarOverlay.addEventListener('click', function() {
            closeSidebar();
        });
    }

    // Close sidebar when clicking a menu link on mobile
    if (sidebar) {
        sidebar.querySelectorAll('.menu-item a').forEach(link => {
            link.addEventListener('click', function() {
                if (window.innerWidth <= 991) {
                    closeSidebar();
                }
            });
        });
    }
});

function createHamburgerButton() {
    const topHeader = document.querySelector('.top-header');
    if (!topHeader) return;

    const hamburger = document.createElement('button');
    hamburger.id = 'hamburgerBtn';
    hamburger.className = 'hamburger-btn';
    hamburger.innerHTML = '<i class="bi bi-list"></i>';
    hamburger.setAttribute('aria-label', 'Toggle sidebar menu');
    
    topHeader.insertBefore(hamburger, topHeader.firstChild);
}

function toggleSidebar() {
    const sidebar = document.querySelector('.sidebar');
    const overlay = document.querySelector('.sidebar-overlay');
    
    if (sidebar && overlay) {
        sidebar.classList.toggle('show');
        overlay.classList.toggle('show');
        
        // Prevent body scroll when sidebar is open
        document.body.style.overflow = sidebar.classList.contains('show') ? 'hidden' : '';
    }
}

function closeSidebar() {
    const sidebar = document.querySelector('.sidebar');
    const overlay = document.querySelector('.sidebar-overlay');
    
    if (sidebar && overlay) {
        sidebar.classList.remove('show');
        overlay.classList.remove('show');
        document.body.style.overflow = '';
    }
}

// ========================================
// USER DROPDOWN
// ========================================

document.addEventListener('click', function(e) {
    const userInfo = document.querySelector('.user-info');
    const userDropdown = document.querySelector('.user-dropdown');
    
    if (userInfo && e.target.closest('.user-info')) {
        if (userDropdown) {
            userDropdown.classList.toggle('show');
        }
    } else if (userDropdown) {
        userDropdown.classList.remove('show');
    }
});

// ========================================
// AUTO-DISMISS ALERTS
// ========================================

document.addEventListener('DOMContentLoaded', function() {
    const alerts = document.querySelectorAll('.alert.alert-dismissible');
    
    alerts.forEach(alert => {
        setTimeout(() => {
            const bsAlert = new bootstrap.Alert(alert);
            bsAlert.close();
        }, 5000); // Auto dismiss after 5 seconds
    });
});

// ========================================
// FORM VALIDATION ENHANCEMENTS
// ========================================

// Add Bootstrap validation classes
document.addEventListener('DOMContentLoaded', function() {
    const forms = document.querySelectorAll('form[data-client-validation="true"]');
    
    forms.forEach(form => {
        form.addEventListener('submit', function(e) {
            if (!form.checkValidity()) {
                e.preventDefault();
                e.stopPropagation();
            }
            form.classList.add('was-validated');
        });
    });
});

// ========================================
// CONFIRMATION DIALOGS
// ========================================

function confirmDelete(message = 'Bạn có chắc chắn muốn xóa?') {
    return confirm(message);
}

// Add confirmation to delete buttons
document.addEventListener('DOMContentLoaded', function() {
    const deleteButtons = document.querySelectorAll('[data-confirm-delete]');
    
    deleteButtons.forEach(button => {
        button.addEventListener('click', function(e) {
            const message = this.getAttribute('data-confirm-delete') || 'Bạn có chắc chắn muốn xóa?';
            if (!confirm(message)) {
                e.preventDefault();
            }
        });
    });
});

// ========================================
// TABLE UTILITIES
// ========================================

// Make tables responsive by wrapping them
document.addEventListener('DOMContentLoaded', function() {
    const tables = document.querySelectorAll('table:not(.table-responsive table)');
    
    tables.forEach(table => {
        if (!table.parentElement.classList.contains('table-responsive')) {
            const wrapper = document.createElement('div');
            wrapper.className = 'table-responsive';
            table.parentNode.insertBefore(wrapper, table);
            wrapper.appendChild(table);
        }
    });
});

// ========================================
// WINDOW RESIZE HANDLER
// ========================================

let resizeTimer;
window.addEventListener('resize', function() {
    clearTimeout(resizeTimer);
    resizeTimer = setTimeout(function() {
        // Close sidebar if window is resized to desktop
        if (window.innerWidth > 991) {
            closeSidebar();
        }
        
        // Recreate or remove hamburger button based on screen size
        const hamburger = document.getElementById('hamburgerBtn');
        if (window.innerWidth <= 991 && !hamburger) {
            createHamburgerButton();
        } else if (window.innerWidth > 991 && hamburger) {
            hamburger.remove();
        }
    }, 250);
});

// ========================================
// UTILITY FUNCTIONS
// ========================================

// Show success message
function showSuccess(message) {
    showAlert(message, 'success');
}

// Show error message
function showError(message) {
    showAlert(message, 'danger');
}

// Generic alert function
function showAlert(message, type = 'info') {
    const contentWrapper = document.querySelector('.content-wrapper');
    if (!contentWrapper) return;
    
    const alert = document.createElement('div');
    alert.className = `alert alert-${type} alert-dismissible fade show`;
    alert.role = 'alert';
    
    const icon = type === 'success' ? 'bi-check-circle-fill' : 
                 type === 'danger' ? 'bi-exclamation-circle-fill' : 
                 'bi-info-circle-fill';
    
    alert.innerHTML = `
        <i class="bi ${icon} me-2"></i>${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    
    contentWrapper.insertBefore(alert, contentWrapper.firstChild);
    
    // Auto dismiss after 5 seconds
    setTimeout(() => {
        const bsAlert = new bootstrap.Alert(alert);
        bsAlert.close();
    }, 5000);
}

// Export functions for use in other scripts
window.CoffeSolution = {
    showSuccess,
    showError,
    showAlert,
    confirmDelete,
    toggleSidebar,
    closeSidebar
};
