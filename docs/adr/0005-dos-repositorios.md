# ADR-005: Dos repositorios separados (backend y frontend)

- **Estado:** Aceptado
- **Fecha:** 2026-06

## Contexto

El sistema tiene dos aplicaciones: la API (.NET) y el cliente (Angular). El
enunciado pide "repositorio público" como entregable, sin especificar uno o
varios.

## Decisión

Se usan **dos repositorios independientes** (poly-repo):

- `eventosvivos-api` — backend .NET.
- `eventosvivos-web` — frontend Angular.

Los ADRs del sistema viven en el repo de backend (núcleo arquitectónico) como
fuente única de verdad; el README del frontend los enlaza.

## Justificación

- Ciclos de vida y despliegues independientes: la API va a Azure App Service y el
  cliente a Azure Static Web Apps, con pipelines de CI/CD separados.
- Historiales de Git limpios y enfocados por tecnología.
- Refleja una práctica habitual en equipos donde backend y frontend evolucionan
  por separado.

## Alternativas descartadas

- **Monorepo**: simplifica el versionado conjunto, pero mezcla dos toolchains
  (dotnet / npm) y dos pipelines en un mismo historial; para esta prueba aporta
  menos claridad que la separación.
- **Tercer repo solo de documentación**: ceremonia innecesaria para el plazo.

## Consecuencias

El contrato de la API (DTOs) no se comparte por código entre repos. Para el
alcance de la prueba, las interfaces TypeScript del frontend se definen a mano,
espejo de los DTOs del backend, y se documenta así (no se genera cliente desde
OpenAPI para evitar ceremonia desproporcionada).
