# ADR-007: Estrategia de seguridad de la API

## Estado
Aceptado

## Contexto
La API es pública y, según el enunciado (y el ADR-003), la operación del
comprador no requiere autenticación de usuario: una reserva se identifica con
nombre y correo capturados en el momento, sin cuenta ni login. Aun así, una API
expuesta a internet debe protegerse frente a abuso y accesos indebidos, y deben
distinguirse las operaciones públicas de las de administración.

## Decisión

### Autorización por contexto (no autenticación de usuario)
Se clasifica cada endpoint según quién debe poder ejecutarlo:

- **Públicos** (sin credencial): listar eventos, ver reporte de ocupación, crear
  reserva y cancelar reserva. El identificador de la reserva (GUID no
  adivinable) actúa como "localizador": solo quien lo recibió al reservar puede
  operar sobre ella (patrón equivalente al de gestión de reservas de
  aerolíneas).
- **Administración** (requieren API key en la cabecera `X-Api-Key`): crear
  evento, cancelar evento y confirmar pago. La confirmación de pago se considera
  una acción de operador/pasarela, no del público.

La API key se valida con comparación de tiempo constante y se lee de
configuración (`Security:AdminApiKey`), nunca del repositorio. Si no hay clave
configurada, la superficie administrativa se deshabilita (responde 503) en lugar
de quedar abierta ("seguro por defecto").

### Endurecimiento del perímetro
- **Rate limiting** por IP (ventana fija) para mitigar abuso y DoS básico.
- **CORS** restringido: en producción solo los orígenes configurados; en
  desarrollo, abierto por comodidad.
- **HTTPS** forzado y **HSTS** fuera de desarrollo.
- **Cabeceras de seguridad**: `X-Content-Type-Options`, `X-Frame-Options`,
  `Referrer-Policy`.
- **Swagger** solo en desarrollo o tras flag explícito de configuración.
- **ProblemDetails** sin filtrar detalles internos (los 5xx no exponen
  el mensaje ni el stack trace).
- **Secretos fuera del repositorio**: cadena de conexión y API key por variables
  de entorno / configuración local ignorada por git. En Azure, conexión a
  PostgreSQL preferentemente por Managed Identity (sin contraseña).

### Validación de entrada
Validación de entrada con FluentValidation antes de los handlers; las reglas de
negocio las garantiza el dominio. La autenticación se evalúa antes que la
validación (no se procesa input de llamadas no autorizadas).

## Fuera de alcance (consciente)
- **Autenticación de usuarios finales** (registro/login, JWT, refresh tokens,
  roles): excluida por el enunciado (ADR-003). Reservar es una acción anónima.
- **Gestión de identidades para organizadores**: la superficie administrativa se
  protege con API key como medida proporcionada. La evolución natural sería
  identidad federada (OAuth2/OIDC) con roles para los organizadores.
- **WAF, cifrado a nivel de campo y auditoría avanzada**: desproporcionados para
  el alcance y el plazo.

## Consecuencias
- La superficie sensible queda protegida sin contradecir el "sin login" del
  enunciado.
- Las decisiones y sus límites quedan explícitos y justificados.
- La ruta de evolución hacia identidad federada está identificada.
