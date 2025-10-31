/* wwwroot/js/question-bank-script.js */

// تبديل المظهر
const themeToggle = document.getElementById('themeToggle');
const body = document.body;

function applyTheme(theme) {
    body.dataset.theme = theme;
    themeToggle.textContent = theme === 'dark' ? '☀️' : '🌙';
    localStorage.setItem('theme', theme);
}

themeToggle.addEventListener('click', () => {
    const currentTheme = body.dataset.theme;
    const newTheme = currentTheme === 'dark' ? 'light' : 'dark';
    applyTheme(newTheme);
});

// تحميل المظهر المفضل عند بدء التشغيل
document.addEventListener('DOMContentLoaded', () => {
    const savedTheme = localStorage.getItem('theme') || 'light';
    applyTheme(savedTheme);
});