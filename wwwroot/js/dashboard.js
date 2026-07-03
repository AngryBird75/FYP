/* ══════════════════════════════════════════════════
   AspiraHub — Dashboard JavaScript
   ══════════════════════════════════════════════════ */

// ── Progress bar animation ───────────────────────
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.progress-fill').forEach(bar => {
        const width = bar.style.width;
        bar.style.width = '0%';
        setTimeout(() => { bar.style.width = width; }, 200);
    });

    document.querySelectorAll('.dist-bar').forEach(bar => {
        const width = bar.style.width;
        bar.style.width = '0%';
        setTimeout(() => { bar.style.width = width; }, 300);
    });

    // Stat counter animation
    document.querySelectorAll('.stat-val').forEach(el => {
        const target = parseInt(el.textContent);
        if (isNaN(target) || target === 0) return;
        let current = 0;
        const step = Math.ceil(target / 30);
        const timer = setInterval(() => {
            current = Math.min(current + step, target);
            el.textContent = current;
            if (current >= target) clearInterval(timer);
        }, 30);
    });
});

// ── Notification toggle ──────────────────────────
function toggleNotif() {
    const panel = document.getElementById('notifPanel');
    if (panel) panel.classList.toggle('open');
}

document.addEventListener('click', e => {
    const panel = document.getElementById('notifPanel');
    if (!panel) return;
    if (!e.target.closest('.notif-btn') &&
        !e.target.closest('.notif-panel')) {
        panel.classList.remove('open');
    }
});

// ── Toast notification ───────────────────────────
function showToast(msg, type = 'info') {
    const existing = document.querySelector('.toast');
    if (existing) existing.remove();

    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.style.background = type === 'error' ? '#ef4444' :
        type === 'success' ? '#10b981' : '#3b82f6';
    toast.textContent = msg;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3200);
}