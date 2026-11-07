// Calendario de fechas de estadía (Flatpickr) — Nueva Reserva y Editar reserva.
// Los inputs originales #FechaIngreso y #FechaSalida siguen guardando el valor en formato yyyy-mm-dd
// (lo que lee el resto del código y lo que se envía al servidor). Flatpickr muestra un input visible dd/mm/aaaa.
(function () {
    let fpIngreso = null, fpSalida = null;

    function sumarDiaISO(iso) {
        const d = new Date(iso + 'T00:00:00');
        d.setDate(d.getDate() + 1);
        const p = n => String(n).padStart(2, '0');
        return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
    }

    const opcionesBase = {
        locale: 'es',
        dateFormat: 'Y-m-d',
        altInput: true,
        altFormat: 'd/m/Y',
        altInputClass: 'form-control fecha-estadia',
        disableMobile: true,   // clave: sin esto, en celulares vuelve a usar el calendario nativo
        allowInput: false
    };

    function init() {
        const ing = document.getElementById('FechaIngreso');
        const sal = document.getElementById('FechaSalida');
        if (!ing || !sal || typeof flatpickr === 'undefined') return;

        fpIngreso = flatpickr(ing, { ...opcionesBase });
        fpSalida  = flatpickr(sal, {
            ...opcionesBase,
            // Sin salida elegida: abrir en el mes del primer día disponible
            onOpen: function (selectedDates, dateStr, inst) {
                if (!inst.selectedDates.length && inst.config.minDate) inst.jumpToDate(inst.config.minDate);
            },
            // Resaltar el primer día disponible (como Chrome de escritorio)
            onDayCreate: function (dObj, dStr, inst, dayElem) {
                if (!inst.selectedDates.length && inst.config.minDate &&
                    dayElem.dateObj.getTime() === inst.config.minDate.getTime())
                    dayElem.classList.add('primer-dia-disponible');
            }
        });

        fpIngreso.altInput.placeholder = 'dd/mm/aaaa';
        fpSalida.altInput.placeholder  = 'dd/mm/aaaa';

        setSalidaHabilitada(!sal.disabled);
        if (ing.value) setMinSalida(ing.value);
    }

    // Mínimo de la salida = día siguiente a la llegada (null = sin mínimo)
    function setMinSalida(isoIngreso) {
        if (!fpSalida) return;
        fpSalida.set('minDate', isoIngreso ? sumarDiaISO(isoIngreso) : null);
    }

    // Vacía la salida SIN disparar el evento change
    function limpiarSalida() {
        if (fpSalida) fpSalida.clear(false);
        else { const s = document.getElementById('FechaSalida'); if (s) s.value = ''; }
    }

    function setSalidaHabilitada(on) {
        const sal = document.getElementById('FechaSalida');
        if (sal) sal.disabled = !on;
        if (fpSalida && fpSalida.altInput) fpSalida.altInput.disabled = !on;
    }

    window.CalEstadia = { init, setMinSalida, limpiarSalida, setSalidaHabilitada, sumarDiaISO };
})();
