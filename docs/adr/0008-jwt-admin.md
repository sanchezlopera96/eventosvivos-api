# ADR-008: Autenticación de administrador con JWT

- **Estado:** Aceptado
- **Fecha:** 2026-06
- **Supersede a:** ADR-003 (autenticación fuera de alcance) y refina la sección de autorización del ADR-007.

## Contexto

El ADR-003 dejó la autenticación fuera de alcance y el ADR-007 propuso proteger la superficie administrativa con una **API key** (`X-Api-Key`). Esa decisión era razonable para una API sin interfaz, pero el alcance evolucionó: se construyó un **panel de administración en Angular** (login, gestión de eventos, confirmación de pagos, reportes). Una API key estática, pensada para máquinas, no encaja con un operador humano que inicia sesión desde un navegador:

- No hay concepto de "sesión" ni de expiración.
- Distribuir y rotar una key compartida en un cliente web es frágil.
- No habilita una evolución natural hacia roles o múltiples administradores.

## Decisión

La superficie de administración se protege con **JWT (Bearer)**:

- `POST /api/auth/login` valida usuario y contraseña (hash **BCrypt**) y emite un token firmado con expiración.
- Los endpoints de administración exigen `RequireAuthorization("Admin")`; el token viaja en `Authorization: Bearer`.
- Las credenciales y la clave de firma se leen de configuración (`AdminCredentials__*`, `Jwt__*`), nunca del repositorio.
- **Seguro por defecto**: si no hay credenciales configuradas, el login responde 503 y la superficie administrativa queda inaccesible, en lugar de abierta.

La superficie pública (listar eventos, ver reporte, crear/cancelar/buscar reservas) sigue **sin credenciales**, como define el ADR-003 para la acción del comprador.

## Justificación

JWT es el estándar de industria para autenticar un SPA contra una API stateless: expiración incorporada, sin estado de servidor, y base directa para añadir _claims_/roles. El hash BCrypt protege la contraseña en reposo. La decisión mantiene el "sin login para el comprador" del enunciado y solo añade identidad donde aporta valor real: la operación administrativa.

## Consecuencias

- El filtro de API key (`AdminApiKeyFilter`) queda obsoleto y debe retirarse del código.
- El ADR-003 se marca como **supersedido**: la auth ya no está fuera de alcance para el administrador.
- Evolución futura (fuera de alcance): roles, refresh tokens e identidad federada (OAuth2/OIDC) para múltiples organizadores.
