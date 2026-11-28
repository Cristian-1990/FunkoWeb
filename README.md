# FunkoApp

Tienda de Funkos de la unidad 3 (Desarrollo de páginas web dinámicas en .NET), 2º DAW. La misma aplicación se construye en dos visiones, Razor Pages y MVC, sobre un núcleo común.

## Estructura

| Proyecto | Qué es |
|----------|--------|
| `FunkoApp` | Visión Razor Pages |
| `FunkoAppMvc` | Visión MVC |
| `FunkoApp.Shared` | Núcleo común: modelo, servicios y datos que usan las dos visiones |
| `FunkoApp.Test` | Pruebas con NUnit, Moq y FluentAssertions |

## Cómo se ejecuta

Requiere el SDK de .NET 10.

```powershell
dotnet run --project FunkoApp
```

Escucha en http://localhost:5147.

```powershell
dotnet run --project FunkoAppMvc
```

Escucha en http://localhost:5276.

## Cómo se prueba

```powershell
dotnet test
```

Todavía no hay pruebas: el proyecto de tests solo tiene el esqueleto.

## Documentación

El diseño en papel (modelo de datos y mapa de páginas) está en [docs/diseno.md](docs/diseno.md).

## Decisiones tomadas

- **Núcleo compartido.** El enunciado dibuja las dos aplicaciones con su propio `Services/` y `Models/`. Se ha optado por un proyecto `FunkoApp.Shared`, como en las soluciones 03 y 04 del profesor, para escribir el negocio una sola vez.
- **Avisos como errores.** Los proyectos compilan con `TreatWarningsAsErrors`. Se silencia `CS1591` (falta de comentario XML), como hace el profesor; la documentación XML se escribe igualmente.
- **Alcance.** Solo se cubre el temario hasta el punto 16. Quedan fuera usuarios e Identity, cesta y estado, internacionalización, caché, Docker y Playwright.

## Estado

Hecho: diseño en papel y esqueleto de la solución. Las dos aplicaciones arrancan con la plantilla por defecto.
