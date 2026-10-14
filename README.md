# Backend - Horario SENATI API (.NET Core & SQL Server)

API RESTful desarrollada con **.NET 8**, **Entity Framework Core** y **SQL Server**.

---

## 📁 Estructura del Proyecto

```
backend/
│
├── Controllers/
│   ├── DocentesController.cs    # CRUD completo de Docentes
│   ├── CursosController.cs      # CRUD completo de Cursos
│   ├── AulasController.cs       # CRUD completo de Aulas
│   └── HorariosController.cs    # CRUD y filtros de Horarios
│
├── Data/
│   └── AppDbContext.cs          # Contexto de base de datos EF Core
│
├── DTOs/                        # Request y Response Data Transfer Objects
│   ├── DocenteDto.cs
│   ├── CursoDto.cs
│   ├── AulaDto.cs
│   └── HorarioDto.cs
│
├── Migrations/                  # Migraciones generadas por EF Core
│
├── Models/                      # Entidades del Dominio
│   ├── Docente.cs
│   ├── Curso.cs
│   ├── Aula.cs
│   └── Horario.cs
│
├── appsettings.json             # Cadena de conexión y configuración
└── Program.cs                   # Configuración del servidor, CORS, Swagger y DI
```

---

## ⚙️ Configuración de la Base de Datos

En `appsettings.json`, asegúrate de que la cadena de conexión coincida con tu instancia de SQL Server:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=HorarioSenatiDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
}
```

> **Nota:** Si utilizas SQL Server con autenticación de usuario (sa) o SQL Server Express:
> - **SQL Express:** `"Server=localhost\\SQLEXPRESS;Database=HorarioSenatiDb;Trusted_Connection=True;TrustServerCertificate=True;"`
> - **Usuario y contraseña:** `"Server=localhost;Database=HorarioSenatiDb;User Id=sa;Password=TuPassword;TrustServerCertificate=True;"`

---

## 🚀 Comandos Principales

1. **Aplicar migraciones para crear la base de datos y tablas:**
   ```bash
   cd backend
   dotnet ef database update
   ```

2. **Ejecutar la API en desarrollo:**
   ```bash
   dotnet run
   ```

3. **Acceder a la documentación Swagger:**
   Abre tu navegador en:
   ```
   https://localhost:7000/
   http://localhost:5000/
   ```

---

## 📡 Endpoints de la API

| Método | Endpoint | Descripción |
|---|---|---|
| **GET** | `/api/docentes` | Lista todos los docentes (permite filtro `?soloActivos=true`) |
| **GET** | `/api/docentes/{id}` | Obtiene un docente por su ID |
| **POST** | `/api/docentes` | Crea un nuevo docente |
| **PUT** | `/api/docentes/{id}` | Actualiza un docente |
| **DELETE** | `/api/docentes/{id}` | Elimina un docente |
| **GET** | `/api/cursos` | Lista todos los cursos |
| **POST** | `/api/cursos` | Registra un nuevo curso |
| **GET** | `/api/aulas` | Lista todas las aulas y laboratorios |
| **POST** | `/api/aulas` | Registra una nueva aula |
| **GET** | `/api/horarios` | Lista horarios con filtros (`?diaSemana=`, `?periodo=`, `?docenteId=`, etc.) |
| **POST** | `/api/horarios` | Asigna un nuevo horario (valida cruces y referencias) |
