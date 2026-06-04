# 🏨 Hotel y Cabañas Brisas de Oro — Sistema de Gestión (Backend)

Sistema web integral de gestión de reservas para Hotel y Cabañas Brisas de Oro, complejo familiar ubicado en Villa Carlos Paz, Córdoba, Argentina.

Este repositorio corresponde al panel de administración desarrollado con ASP.NET Core MVC, diseñado para uso diario en el complejo y como proyecto de tesis universitaria (Analista de Sistemas).

Desarrollado en equipo como proyecto final de la carrera Analista de Sistemas.

---

## ✨ Funcionalidades

- Sistema de autenticación con roles (Administrador y Viewer)
- Gestión completa de reservas con validación de disponibilidad en tiempo real
- Calendario tipo Gantt con visualización de todas las instalaciones
- Módulo de pagos con historial y control de saldos
- Panel de inicio con resumen operativo diario
- Dashboard con métricas de ocupación e ingresos
- Módulo de facturación con movimientos y saldos pendientes
- Gestión de alojamientos y tarifas por temporada
- Diseño responsive 100% funcional en desktop y mobile

---

## 🛠 Tecnologías

- ASP.NET Core MVC (.NET 7)
- SQL Server 2022 Express
- Entity Framework Core
- ASP.NET Core Identity
- HTML / CSS / Bootstrap 5
- JavaScript

---

## 🚀 Correr localmente

```bash
# Restaurar dependencias
dotnet restore

# Aplicar migraciones
dotnet ef database update

# Correr el servidor
dotnet run
```

Abrí `http://localhost:[puerto]` en el navegador.

---

## 📁 Estructura

```
brisas-backend/
├── Controllers/        ← Controladores MVC
├── Models/             ← Modelos y ViewModels
├── Views/              ← Vistas Razor (.cshtml)
├── Data/               ← DbContext y migraciones
├── wwwroot/            ← Archivos estáticos (CSS, JS, imágenes)
├── Properties/         ← Configuración de lanzamiento
└── appsettings.json    ← Configuración de conexión y app
```

---

## ⚙️ Configuración

La cadena de conexión se configura en `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=BrisasDeOro;Trusted_Connection=True;TrustServerCertificate=True"
}
```

---

## 📋 Módulos del sistema

- **Inicio** — Resumen operativo del día: check-ins, check-outs, limpieza, desayunos y pendientes
- **Dashboard** — Métricas de ocupación, ingresos por método de pago y canal de origen
- **Calendario** — Vista Gantt de todas las instalaciones con reservas por estado de pago
- **Reservas** — Alta, edición, cancelación y detalle de reservas
- **Pagos** — Registro de pagos con historial por reserva
- **Facturación** — Movimientos y saldos pendientes
- **Alojamientos** — Gestión de habitaciones, aparts y cabañas
- **Tarifas** — Precios por temporada, cantidad de personas y servicio de desayuno
- **Usuarios** — Gestión de accesos con roles Administrador y Viewer

---

## 🔜 Próximos pasos

- Integración del backend con el frontend Next.js ya desarrollado
- Migración del backend a API Routes de Next.js con Prisma
- Base de datos MongoDB
- Publicación en Railway
- Dominio brisasdeoro.com.ar

---

*Proyecto de tesis — Analista de Sistemas, IRESM*
