/* ══════════════════════════════════════════════════
   AspiraHub — Auth JavaScript
   Dark/Light Toggle + Interactions
   ══════════════════════════════════════════════════ */

// ── Theme ────────────────────────────────────────
function toggleTheme() {
    const html = document.documentElement;
    const isDark = html.getAttribute('data-theme') === 'dark';
    const next = isDark ? 'light' : 'dark';
    html.setAttribute('data-theme', next);
    localStorage.setItem('ah-theme', next);

    const icon = document.querySelector('.theme-icon');
    if (icon) icon.textContent = next === 'dark' ? '🌙' : '☀️';
}

// Load saved theme
(function () {
    const saved = localStorage.getItem('ah-theme') || 'dark';
    document.documentElement.setAttribute('data-theme', saved);
    const icon = document.querySelector('.theme-icon');
    if (icon) icon.textContent = saved === 'dark' ? '🌙' : '☀️';
})();

// ── Eye Toggle ───────────────────────────────────
function toggleEye(inputId, btn) {
    const input = document.getElementById(inputId);
    if (!input) return;
    input.type = input.type === 'password' ? 'text' : 'password';
    btn.textContent = input.type === 'password' ? '👁' : '🙈';
}

// ── Role Selection ───────────────────────────────
function selectRole(role) {
    document.getElementById('card-student')
        ?.classList.toggle('selected', role === 'student');
    document.getElementById('card-company')
        ?.classList.toggle('selected', role === 'company');
}

// ── Tag Pill Toggle (checkboxes) ─────────────────
document.addEventListener('DOMContentLoaded', () => {
    document.querySelectorAll('.tag-pill').forEach(pill => {
        const input = pill.querySelector('input[type="checkbox"]');
        if (input) {
            pill.addEventListener('click', () => {
                pill.classList.toggle('sel', input.checked);
            });
        }
    });

    // Step bar — auto mark step
    const stepEl = document.querySelector('[data-step]');
    if (stepEl) {
        const step = parseInt(stepEl.dataset.step);
        document.querySelectorAll('.sp-dot').forEach((dot, i) => {
            if (i < step - 1) dot.classList.add('done');
            else if (i === step - 1) dot.classList.add('active');
        });
        document.querySelectorAll('.sp-line').forEach((line, i) => {
            if (i < step - 1) line.classList.add('done');
        });
    }

    // Auto-select first option card
    const firstOpt = document.querySelector('.option-card input');
    if (firstOpt) firstOpt.checked = true;
});

// ── Toast Notification ───────────────────────────
function showToast(msg) {
    const existing = document.querySelector('.toast');
    if (existing) existing.remove();
    const toast = document.createElement('div');
    toast.className = 'toast';
    toast.textContent = msg;
    document.body.appendChild(toast);
    setTimeout(() => toast.remove(), 3200);
}

// ── Copy Key ─────────────────────────────────────
function copyKey() {
    const key = document.getElementById('theKey')?.textContent;
    if (!key) return;
    navigator.clipboard.writeText(key).then(() => {
        showToast('🔑 Key copied! Keep it safe.');
    });
}

// ── Option Card click ────────────────────────────
document.addEventListener('click', e => {
    const card = e.target.closest('.option-card');
    if (card) {
        document.querySelectorAll('.option-card').forEach(c => c.classList.remove('selected'));
        card.classList.add('selected');
        const input = card.querySelector('input');
        if (input) input.checked = true;
    }
});

// ── Form validation feedback ─────────────────────
document.addEventListener('submit', e => {
    const btn = e.target.querySelector('button[type="submit"]');
    if (btn) {
        btn.disabled = true;
        btn.innerHTML = '<span>Processing...</span>';
        setTimeout(() => {
            btn.disabled = false;
        }, 3000);
    }
});