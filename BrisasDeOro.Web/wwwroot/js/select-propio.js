// Desplegable propio para todos los <select class="form-select"> del sistema.
// El <select> original queda oculto y sigue siendo la fuente de datos.
// Excluir un select puntual: agregarle el atributo data-nativo.
(function () {
    const valueDesc = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, 'value');
    const indexDesc = Object.getOwnPropertyDescriptor(HTMLSelectElement.prototype, 'selectedIndex');
    let abierto = null; // { select, menu, btn }

    function textoSeleccionado(select) {
        const opt = select.options[select.selectedIndex];
        return opt ? opt.textContent.trim() : '';
    }

    function sincronizar(select) {
        const btn = select._spBtn;
        if (!btn) return;
        btn.textContent = textoSeleccionado(select) || ' ';
        btn.disabled = select.disabled;
        if (select.disabled && abierto && abierto.select === select) cerrar();
    }

    function cerrar() {
        if (!abierto) return;
        abierto.menu.remove();
        abierto.btn.setAttribute('aria-expanded', 'false');
        abierto = null;
    }

    function posicionar(btn, menu) {
        const r      = btn.getBoundingClientRect();
        const abajo  = window.innerHeight - r.bottom - 8;
        const arriba = r.top - 8;
        const alto   = Math.min(menu.scrollHeight, 260);
        menu.style.minWidth = r.width + 'px';
        const ancho = Math.max(r.width, menu.offsetWidth);
        menu.style.left = Math.max(8, Math.min(r.left, window.innerWidth - ancho - 8)) + 'px';
        // Abre hacia abajo; solo hacia arriba si abajo no hay lugar
        if (abajo >= Math.min(alto, 160) || abajo >= arriba) {
            menu.style.top = (r.bottom + 2) + 'px';
            menu.style.maxHeight = Math.max(120, Math.min(260, abajo)) + 'px';
        } else {
            const h = Math.min(260, arriba);
            menu.style.maxHeight = h + 'px';
            menu.style.top = (r.top - 2 - Math.min(alto, h)) + 'px';
        }
    }

    function abrir(select) {
        cerrar();
        const btn  = select._spBtn;
        const menu = document.createElement('div');
        menu.className = 'sp-menu';
        menu.setAttribute('role', 'listbox');
        menu.addEventListener('click', e => e.stopPropagation());
        Array.from(select.options).forEach(opt => {
            if (opt.hidden) return;
            const item = document.createElement('div');
            item.className = 'sp-item' + (opt.selected ? ' active' : '') + (opt.disabled ? ' disabled' : '');
            item.textContent = opt.textContent.trim() || ' ';
            item.setAttribute('role', 'option');
            if (!opt.disabled) {
                item.addEventListener('click', function () {
                    const cambio = select.value !== opt.value;
                    select.value = opt.value;
                    cerrar();
                    if (cambio) select.dispatchEvent(new Event('change', { bubbles: true }));
                });
            }
            menu.appendChild(item);
        });
        document.body.appendChild(menu);
        posicionar(btn, menu);
        btn.setAttribute('aria-expanded', 'true');
        abierto = { select, menu, btn };
        const act = menu.querySelector('.sp-item.active');
        if (act) menu.scrollTop = act.offsetTop - menu.clientHeight / 2 + act.offsetHeight / 2;
    }

    function mejorar(select) {
        if (select._spBtn || select.multiple || select.hasAttribute('data-nativo')) return;
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = select.className + ' sp-btn text-start';
        btn.setAttribute('aria-haspopup', 'listbox');
        btn.setAttribute('aria-expanded', 'false');
        if (select.getAttribute('style')) btn.setAttribute('style', select.getAttribute('style'));
        if (select.id) btn.id = select.id + '-sp';
        select.insertAdjacentElement('afterend', btn);
        select.classList.add('d-none');
        select._spBtn = btn;

        btn.addEventListener('click', function (e) {
            e.stopPropagation();
            if (abierto && abierto.select === select) cerrar(); else abrir(select);
        });
        select.addEventListener('change', () => sincronizar(select));
        new MutationObserver(() => sincronizar(select))
            .observe(select, { childList: true, subtree: true, characterData: true, attributes: true, attributeFilter: ['disabled'] });
        // Asignaciones por código (select.value = ... / selectedIndex = ...) también actualizan el botón
        Object.defineProperty(select, 'value', {
            get() { return valueDesc.get.call(this); },
            set(v) { valueDesc.set.call(this, v); sincronizar(this); },
            configurable: true
        });
        Object.defineProperty(select, 'selectedIndex', {
            get() { return indexDesc.get.call(this); },
            set(v) { indexDesc.set.call(this, v); sincronizar(this); },
            configurable: true
        });
        if (select.id) {
            document.querySelectorAll(`label[for="${select.id}"]`).forEach(l =>
                l.addEventListener('click', e => { e.preventDefault(); btn.focus(); }));
        }
        sincronizar(select);
    }

    function init(root) {
        (root || document).querySelectorAll('select.form-select').forEach(mejorar);
    }

    document.addEventListener('click', cerrar);
    document.addEventListener('keydown', e => { if (e.key === 'Escape') cerrar(); });
    window.addEventListener('resize', cerrar);
    window.addEventListener('scroll', e => { if (abierto && !abierto.menu.contains(e.target)) cerrar(); }, true);
    document.addEventListener('DOMContentLoaded', () => init());

    window.SelectPropio = { init, mejorar, sincronizar };
})();
