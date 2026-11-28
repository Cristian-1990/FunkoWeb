# FunkoApp

Tienda de Funkos de la unidad 3 (Desarrollo de páginas web dinámicas en .NET), 2º DAW. La misma aplicación se construye en dos visiones, Razor Pages y MVC, sobre un núcleo común.

## Estructura

| Proyecto | Qué es |
|----------|--------|
| `FunkoApp` | Visión Razor Pages |
| `FunkoAppMvc` | Visión MVC |
| `FunkoApp.Shared` | Núcleo común: modelo, datos, servicios e infraestructura que usan las dos visiones |
| `FunkoApp.Test` | Pruebas con NUnit, Moq y FluentAssertions |

## Arquitectura

Las dos visiones solo se ocupan de la presentación. El negocio y los datos viven en `FunkoApp.Shared`.

```mermaid
flowchart LR
    UI[Razor Pages o MVC] --> Servicio[IFunkoService]
    Servicio --> Contexto[FunkoDbContext]
    Contexto --> BD[(SQLite)]
    Semilla[FunkoSeeder] --> Contexto
```

El servicio devuelve DTOs, nunca la entidad: las vistas no conocen el modelo de la base de datos.

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

En Development la base de datos SQLite (`funkos.db`) se borra y se recrea con datos de ejemplo en cada arranque.

## Cómo se prueba

```powershell
dotnet test
```

Las pruebas usan NUnit y FluentAssertions, con el patrón AAA y las clases `CasosPositivos` y `CasosNegativos`. El servicio se prueba contra EF Core en memoria, con una base de datos nueva por prueba.

## Documentación

El diseño en papel (modelo de datos y mapa de páginas) está en [docs/diseno.md](docs/diseno.md).

## Decisiones tomadas

- **Núcleo compartido.** El enunciado dibuja las dos aplicaciones con su propio `Services/` y `Models/`. Se ha optado por un proyecto `FunkoApp.Shared`, como en las soluciones 03 y 04 del profesor, para escribir el negocio una sola vez.
- **Avisos como errores.** Los proyectos compilan con `TreatWarningsAsErrors`. Se silencia `CS1591` (falta de comentario XML), como hace el profesor; la documentación XML se escribe igualmente.
- **Servicio sin `Result`.** El servicio devuelve DTOs y `null` o `bool`, como el profesor en esta unidad. Es una desviación consciente de la arquitectura con `Result<T, DomainError>` de otros proyectos.
- **Entidad limpia.** Las entidades no llevan atributos: las restricciones de la base de datos se declaran con Fluent API en `FunkoDbContext`. Las validaciones del formulario irán en el ViewModel.
- **Nombres.** Los tipos van en inglés (`Funko`, `Category`) y las propiedades en español (`Nombre`, `Anio`, `PrecioReferencia`), como en el enunciado y en las soluciones del profesor.
- **Borrado de categorías.** Una categoría con Funkos no se puede borrar (`DeleteBehavior.Restrict`).
- **Base de datos.** SQLite en fichero para la aplicación y EF Core en memoria para las pruebas.
- **Alcance.** Solo se cubre el temario hasta el punto 16. Quedan fuera usuarios e Identity, cesta y estado, internacionalización, caché, Docker y Playwright.

## Estado

Hecho:

- Diseño en papel y esqueleto de la solución.
- Modelo (`Funko` y `Category`), `DbContext` con Fluent API, DTOs, mapper y servicio de lectura del catálogo.
- Semilla de ejemplo con SQLite.
- Pruebas del mapper y del servicio.

Pendiente: la portada y el resto de pantallas.