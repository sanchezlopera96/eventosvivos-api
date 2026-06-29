# ADR-003: Autenticación y autorización fuera de alcance

- **Estado:** Supersedido por ADR-008
- **Fecha:** 2026-06

## Contexto

El enunciado describe dos actores —comprador (reserva entradas) y administrador
(confirma pagos)— pero:

- La reserva se identifica por **nombre y email del comprador** (RF-03), no por
  una cuenta autenticada.
- No se mencionan registro, login, credenciales, roles ni tokens en ningún
  requisito funcional ni regla de negocio.
- No hay gestión de usuarios; los venues son datos de referencia preexistentes.

## Decisión

No se implementa autenticación ni autorización. La distinción entre acciones de
comprador y administrador se modela a nivel de endpoints/casos de uso, sin un
sistema de identidad.

## Justificación

Añadir JWT/Identity consumiría tiempo sin responder a ningún requisito, restando
esfuerzo a lo que sí se evalúa: reglas de negocio, casos borde y calidad del
diseño. Es una decisión consciente de alcance, no una omisión.

## Mitigación

La arquitectura queda preparada para incorporar autenticación de forma aditiva:
endpoints aislados que podrían decorarse con autorización; entidades que no
asumen ausencia de usuario. Se mantienen las buenas prácticas OWASP aplicables sin
auth: validación de entrada, sanitización, manejo seguro de errores, CORS.
