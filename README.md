# Brisas de Oro — Sistema de Gestión Interno

Sistema de gestión interno para Hotel y Cabañas Brisas de Oro, Villa Carlos Paz, Córdoba. Desarrollado como proyecto de tesis de la carrera Analista de Sistemas en IRESM.

> Versión preparada para deployment en Railway con PostgreSQL.  
> [Ver sitio web público →](https://brisas-de-oro-next.vercel.app)

## 🚀 Estado del proyecto

En proceso de migración a Railway con PostgreSQL. La versión original corre localmente con SQL Server.

## ✨ Módulos del sistema

- Login y autenticación con roles (Administrador / Viewer)
- Inicio — operativa diaria: check-ins, check-outs, limpieza, desayuno, cobros urgentes
- Dashboard — métricas: ingresos, ocupación, canales de origen, comparación de períodos
- Calendario — vista Gantt de reservas con navegación por fechas
- Reservas — listado, detalle, edición y cancelación
- Facturación — movimientos financieros y saldos pendientes
- Alojamientos — gestión de las 16 unidades físicas del complejo
- Tarifas — precios por temporada, alojamiento y cantidad de personas
- Usuarios — administración de accesos al sistema
- Nueva Reserva — formulario guiado con disponibilidad en tiempo real

## 🛠️ Tecnologías

- ASP.NET Core MVC (.NET 7)
- Entity Framework Core 7
- PostgreSQL (Railway) / SQL Server 2022 Express (local)
- ASP.NET Core Identity
- Bootstrap 5.3

## ▶️ Correr localmente

```bash
dotnet restore
dotnet run
```

Abrí `http://localhost:5272` en el navegador.

## 📁 Estructura

```
BrisasDeOro.Web/
├── Controllers/    → Lógica de negocio por módulo
├── Models/         → Entidades y ViewModels
├── Views/          → Vistas Razor por módulo
├── Data/           → DbContext y SeedData
├── Migrations/     → Migraciones de EF Core
└── wwwroot/        → Archivos estáticos
```

## 🔮 Próximos pasos

- Completar migración a PostgreSQL en Railway
- Conectar con el sitio web público en producción
- Implementar Swagger completo con la nueva arquitectura
