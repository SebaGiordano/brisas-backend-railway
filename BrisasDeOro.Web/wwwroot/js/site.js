// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ── Botón "ojito": mostrar/ocultar contraseña ──────────────────────────────────
// Reutilizable en cualquier pantalla: envolver el <input> en un .input-group junto
// a un <button class="btn-toggle-password"><i class="bi bi-eye"></i></button>.
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.btn-toggle-password').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var wrapper = btn.closest('.input-group');
            var input = wrapper ? wrapper.querySelector('input') : null;
            if (!input) return;

            var mostrar = input.type === 'password';
            input.type = mostrar ? 'text' : 'password';

            var icon = btn.querySelector('i');
            if (icon) {
                icon.classList.toggle('bi-eye', !mostrar);
                icon.classList.toggle('bi-eye-slash', mostrar);
            }
            btn.setAttribute('aria-label', mostrar ? 'Ocultar contraseña' : 'Mostrar contraseña');
        });
    });
});
