# Brisas de Oro — Sistema de Gestión Interno

Sistema de gestión interno de Hotel y Cabañas Brisas de Oro (Villa Carlos Paz, Córdoba). Centraliza reservas, operativa diaria, facturación y métricas de ocupación de un complejo familiar de 16 unidades (8 cabañas y 8 habitaciones) más una casa con pileta en Icho Cruz.

Proyecto final de Práctica Profesionalizante II — Analista de Sistemas. Integrantes: Giordano Sebastián · Bazán Rocío · Bazán Renzo.

🌐 Sitio web público (Next.js, Vercel): brisas-de-oro-next.vercel.app — repositorio brisas-de-oro-next

## 🚀 Estado del proyecto

Sistema terminado y publicado en Railway con base de datos PostgreSQL. Actualmente en uso real en el hotel, en etapa de transición desde las planillas de cálculo: se cargan las reservas de la temporada anterior (para comparar períodos) y las reservas nuevas.

## ✨ Módulos

| Módulo | Qué resuelve |
|---|---|
| Login y roles | Acceso con usuario y contraseña. Rol Administrador (gestiona todo) y rol Viewer (solo consulta: pensado para los dueños) |
| Inicio | Operativa del día: check-ins y check-outs, comensales del desayuno (hoy y mañana), limpieza calculada por unidad (diaria en habitaciones, cada 3 días en cabañas, cambio de sábanas al 6.º día, limpieza profunda en cada check-out) y cobros pendientes |
| Nueva Reserva | Formulario guiado con disponibilidad en tiempo real y precio sugerido según temporada, unidad y cantidad de personas. Bloquea superposiciones entre aparts y sus habitaciones |
| Reservas | Listado, detalle, edición, cancelación y comprobante en PDF con el estado de pagos actualizado |
| Reservas grupales | Una reserva, un titular y un monto total para varias unidades a la vez |
| Reservas vinculadas | Reservas independientes (cada una con sus pagos) conectadas entre sí: cambio de unidad durante la estadía o familias del mismo grupo que pagan por separado |
| Aire acondicionado adicional | Cobro diario del aire acondicionado por unidad y por día |
| Calendario | Vista tipo Gantt de todas las reservas, con navegación por fechas |
| Facturación | Movimientos (señas, pagos, ajustes) por método de pago y saldos pendientes, con filtros y descarga en PDF |
| Dashboard | Ocupación por unidad y por plaza, separada en cabañas y habitaciones, ingresos prorrateados por día, canales de origen y comparación entre períodos. Excluye las invitaciones. La Casa se mide aparte |
| Tarifas | Precios por temporada, unidad y cantidad de personas (con y sin desayuno). Ajuste masivo por porcentaje con historial y opción de deshacer |
| Alojamientos | Gestión de las unidades, capacidades y relación apart ↔ habitaciones |
| Usuarios | Alta, edición, activación y baja de accesos |
| API de disponibilidad | Endpoint público de solo lectura (GET /api/disponibilidad) que consume el sitio web público. Devuelve solo cantidades de unidades libres por tipo, sin datos de huéspedes |

## 🛠️ Tecnologías

- C# / ASP.NET Core MVC (.NET 7) con vistas Razor
- Entity Framework Core 7 + proveedor Npgsql (PostgreSQL)
- PostgreSQL (Railway)
- ASP.NET Core Identity — usuarios, contraseñas cifradas, sesiones y roles
- QuestPDF — comprobantes y reportes en PDF
- Bootstrap 5.3, Bootstrap Icons, Chart.js y jQuery Validation
- Swagger — documentación de la API (solo en desarrollo)

## 🏗️ Arquitectura

```
Cliente (navegador)
   │
   ▼
Sitio web público — Next.js en Vercel
   │  función de servidor /api/disponibilidad (proxy)
   ▼
Sistema de gestión interno — ASP.NET Core MVC en Railway
   │  Entity Framework Core + Npgsql
   ▼
PostgreSQL en Railway
```

El navegador del cliente nunca se comunica directamente con el sistema de gestión: el sitio web público reenvía la consulta desde su servidor. El único endpoint sin inicio de sesión es el de disponibilidad (solo lectura). Todas las demás acciones requieren login, y las que modifican datos requieren el rol Administrador.

## 🗂️ Historial de la base de datos (migraciones)

- InitialPostgres — estructura inicial
- AgregarReservaGrupal
- AgregarAireAcondicionadoAdicional
- AgregarReservasVinculadas
- AgregarAlojamientoCasa
- AgregarHistorialAjustesPrecio

Las migraciones se aplican automáticamente al iniciar la aplicación.

El proyecto comenzó con SQL Server 2022 Express en local y migró a PostgreSQL en junio de 2026 para publicarse en Railway. Gracias a Entity Framework, la migración consistió en cambiar el proveedor y regenerar las migraciones, sin modificar la lógica del sistema.

## ▶️ Correr localmente

Requisitos: .NET 7 SDK y una base PostgreSQL.

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Port=...;Database=...;Username=...;Password=..."
dotnet restore
dotnet run
```

Abrir http://localhost:5272.

En producción (Railway) la conexión se toma de la variable de entorno DATABASE_URL. Ninguna credencial de base de datos se guarda en el repositorio.

## ⚠️ Notas técnicas

- Fechas y zona horaria: el servidor usa la zona America/Argentina/Buenos_Aires, y las fechas de reservas, tarifas y temporadas se leen con un conversor de Entity Framework para evitar el corrimiento de un día que producía la conversión automática de Npgsql.
- Fuentes para PDF: Open Sans va embebida en el proyecto (licencia OFL) porque los contenedores Linux de Railway no traen fuentes del sistema.
- CORS: solo se permiten http://localhost:3000 y https://brisas-de-oro-next.vercel.app.

## 🔮 Próximos pasos

- Separar una base de datos de desarrollo de la de producción
- Copias de seguridad periódicas de la base
- Actualizar a .NET 8 (LTS)
