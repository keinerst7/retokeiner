# 🚗 Sistema de Recaudos de Vehículos - Reto F2X

Sistema completo de gestión y consulta de recaudos de vehículos desarrollado como parte del reto técnico de F2X.

## 📋 Datos de la Aplicación

- **Nombre:** Sistema de Recaudos de Vehículos
- **Versión:** 1.0.0
- **Descripción:** AplicacióN que permite importar, consultar y generar reportes de recaudos de vehículos desde una API externa, con almacenamiento en base de datos SQL Server y visualización en Angular.

## 🛠 Tecnologías

### Frontend
- **Angular 20.3.0** - Framework principal
- **TypeScript 5.9.2** - Lenguaje de programación
- **RxJS 7.8.0** - Programación reactiva
- **Angular Forms** - Manejo de formularios

### Backend
- **.NET 9.0** - Framework del servidor
- **ASP.NET Core** - Web API REST
- **Entity Framework Core 9.0.9** - ORM para acceso a datos
- **Swashbuckle.AspNetCore 9.0.6** - Documentación OpenAPI/Swagger

### Base de Datos
- **SQL Server** - Motor de base de datos
- **Esquema:** `reto_keiner`
- **Tabla principal:** `recaudos`

## 📂 Estructura del Proyecto

```
reto2/
├── reto2/                          # Backend (C# .NET)
│   ├── Controllers/                # Capa de Presentación
│   │   └── RecaudosController.cs
│   ├── Services/                   # Capa de Lógica de Negocio
│   │   ├── RecaudoService.cs
│   │   └── ExternalApiService.cs
│   ├── Data/                       # Capa de Datos
│   │   └── ApplicationDbContext.cs
│   ├── Models/
│   │   └── Recaudo.cs
│   ├── Program.cs
│   └── appsettings.json
│
└── reto-angular/                   # Frontend (Angular)
    ├── src/
    │   └── app/
    │       ├── components/
    │       │   ├── recaudos-grid/
    │       │   └── reporte-mensual/
    │       ├── services/
    │       │   └── recaudo.service.ts
    │       └── models/
    │           └── recaudo.model.ts
    └── package.json
```

## 🏗 Arquitectura

La aplicación implementa una **arquitectura de 3 capas**:

### Capa de Presentación
- **Controllers** (RecaudoController.cs)
- **Angular Components** (recaudos-grid, reporte-mensual)

### Capa de Lógica de Negocio
- **Services** (RecaudoService, ExternalApiService)
- Implementación de reglas de negocio
- Validaciones y transformaciones de datos

### Capa de Datos
- **ApplicationDbContext** (Entity Framework)
- **Modelos** (Recaudo)
- Acceso a SQL Server

### Patrones de Diseño Implementados
- **Repository Pattern** (a través de DbContext)
- **Service Layer Pattern**
- **Dependency Injection**
- **DTO Pattern** (para comunicación con API externa)


## 🚀 Instalación y Configuración

### Prerrequisitos

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js 20.x o superior](https://nodejs.org/)
- [Angular CLI 20.x](https://angular.dev/tools/cli)
- [SQL Server](https://www.microsoft.com/sql-server)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) o [Visual Studio Code](https://code.visualstudio.com/)
- [Git](https://git-scm.com/)

### 1. Clonar el Repositorio

```bash
git clone https://github.com/keinerst7/retokeiner.git
cd reto2
```

### 2. Configuración del Backend (.NET)

#### 2.1 Configurar cadena de conexión

Edita el archivo `reto2/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=23.23.205.17;Database=reto_keiner;User Id=Keiner;Password=ifa$uf.Y3?ka$j4Cfp;TrustServerCertificate=True;MultipleActiveResultSets=true;Encrypt=False;"
  }
}
```

#### 2.2 Crear la base de datos y esquema

Abre **SQL Server Management Studio** y ejecuta:

```sql
CREATE DATABASE reto_keiner;
GO

USE reto_keiner;
GO

-- El esquema y tabla se crean automáticamente con Entity Framework
```

#### 2.3 Aplicar migraciones

En la terminal, dentro de la carpeta `reto2`:

```bash
cd reto2
dotnet restore
dotnet ef database update
```

Si no tienes Entity Framework CLI instalado:

```bash
dotnet tool install --global dotnet-ef
```

#### 2.4 Ejecutar el backend

```bash
dotnet run
```

El servidor estará disponible en: `http://localhost:5187`

Swagger UI: `http://localhost:5187/index.html`

### 3. Configuración del Frontend (Angular)

#### 3.1 Instalar dependencias

En la terminal, dentro de la carpeta `reto-angular`:

```bash
cd reto-angular
npm install
```

#### 3.2 Verificar configuración de API

El archivo `src/app/services/recaudo.service.ts` ya está configurado para apuntar a:

```typescript
private apiUrl = 'http://localhost:5187/api/Recaudos';
```

Si tu backend corre en otro puerto, actualiza esta URL.

#### 3.3 Ejecutar el frontend

```bash
ng serve
```

La aplicación estará disponible en: `http://localhost:4200`

## 📖 Uso de la Aplicación

### Importar Datos

1. Asegúrate de que el backend esté corriendo
2. Ve a Swagger UI: `http://localhost:5187/index.html`
3. Ejecuta el endpoint: `POST /api/Recaudos/importar`
4. Esto importará todos los datos desde el 31 de mayo de 2024

### Consultar Datos Brutos

1. Abre la aplicación Angular: `http://localhost:4200`
2. Por defecto verás la vista "Datos Brutos"
3. Usa los filtros por **Estación**, **Sentido** o **Categoría**
4. Los datos se filtran automáticamente al escribir

### Ver Reporte Mensual

1. Click en el botón "Reporte Mensual"
2. Selecciona **Año** y **Mes**
3. El reporte se genera automáticamente
4. Verás:
   - Tabla agrupada por estación y fecha
   - Totales por estación
   - Totales generales

## 📊 Endpoints de la API

Consulta la documentación completa en:
- **Swagger UI:** http://localhost:5187
- **OpenAPI Spec:** `openapi.yaml` en la raíz del proyecto

### Principales Endpoints

```
GET    /api/Recaudos                          # Obtener todos los recaudos
GET    /api/Recaudos/estacion/{estacion}      # Filtrar por estación
GET    /api/Recaudos/fecha/{fecha}            # Filtrar por fecha
GET    /api/Recaudos/rango                    # Filtrar por rango de fechas
POST   /api/Recaudos/importar                 # Importar datos desde API externa
POST   /api/Recaudos/importar/{fecha}         # Importar fecha específica
GET    /api/Recaudos/reporte-mensual          # Generar reporte mensual
GET    /api/Recaudos/reporte-estaciones       # Reporte agrupado por estación
```

## 🧪 Probar la API

### Con Postman

1. Importa el archivo `openapi.yaml`
2. Postman generará automáticamente la colección
3. Ejecuta los endpoints

### Con Swagger UI

1. Ve a `http://localhost:5187index.html`
2. Prueba directamente desde la interfaz

## 🐛 Solución de Problemas

### Error de conexión a la base de datos

```
Error: Cannot connect to SQL Server
```

**Solución:**
- Verifica que SQL Server esté corriendo
- Revisa la cadena de conexión en `appsettings.json`
- Asegúrate de tener permisos en la base de datos

### Error de CORS en Angular

```
Error: CORS policy blocked
```

**Solución:**
- Verifica que el backend esté corriendo en el puerto 5187
- El archivo `Program.cs` ya tiene CORS configurado para `http://localhost:4200`

### No se muestran datos en Angular

**Solución:**
1. Verifica que el backend esté corriendo
2. Importa datos con `POST /api/Recaudos/importar`
3. Revisa la consola del navegador (F12) para errores

## 📝 Notas Importantes

- La API externa requiere que las consultas sean de fechas con **más de 2 días de anterioridad**
- Los datos se importan desde el **31 de mayo de 2024** en adelante
- La tabla de base de datos se crea automáticamente con Entity Framework
- El reporte mensual ejecuta la agrupación en el **servidor (SQL Server)**, no en el cliente

## 👨‍💻 Desarrollado por

**Keiner Arenas**

Para el reto de F2X
