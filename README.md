# Entrega final - Escenarios y Tacticas aplicadas al proyecto

## Contexto del proyecto

El proyecto se centra en el desarrollo de un tutor inteligente enfocado en la identificación de debilidades de los estudiantes y la recomendación de contenidos personalizados para mejorar su aprendizaje. El sistema analizará el desempeño de los estudiantes a través de evaluaciones, detectará áreas de mejora y sugerirá recursos educativos específicos para cada estudiante. El tutor mediante rubricas previamente definidas, calificará el desempeño del estudiante y clasificará su nivel de comprensión, permitiendo adaptar las recomendaciones a sus necesidades individuales. El objetivo es proporcionar una experiencia de aprendizaje personalizada y efectiva que ayude a los estudiantes a superar sus dificultades en fundamentos de desarrollo de software.

## Priorización de atributos de calidad

|Atributo de calidad|Importancia (0-5)|Justificación|
|---|---|---|
|Capacidad de interacción|5|Es uno de los atributos más importantes porque los estudiantes interactuarán constantemente con la plataforma. El sistema debe ser intuitivo, claro y fácil de usar para evitar abandono y facilitar el aprendizaje autónomo. Una mala experiencia de usuario comprometería directamente la efectividad del tutor inteligente.|
|Mantenibilidad|5|Los modelos pedagógicos, contenidos y algoritmos de recomendación evolucionarán constantemente. El sistema debe poder modificarse fácilmente para incorporar nuevas estrategias de aprendizaje, corregir reglas y adaptar contenidos sin rehacer toda la arquitectura.|
|Seguridad|4|El sistema manejará datos académicos y posiblemente información personal de los estudiantes. Es importante proteger la confidencialidad y el acceso a los datos, aunque no se trata de información altamente sensible como datos bancarios o clínicos, por lo que no se prioriza con 5.|
|Adecuación funcional|3|El sistema debe cumplir correctamente con las funciones básicas de identificación de debilidades y recomendación de contenidos, pero no requiere una cobertura funcional extremadamente amplia en una primera versión. Se prioriza tener funciones clave bien implementadas antes que incorporar muchas características adicionales.|
|Flexibilidad|3|Es importante permitir futuras adaptaciones a distintos cursos o metodologías educativas, pero inicialmente el enfoque está en validar el funcionamiento principal del tutor inteligente antes de maximizar la adaptabilidad.|
|Compatibilidad|2|Aunque es deseable integrarse con LMS o plataformas educativas externas, inicialmente el sistema puede operar de manera independiente. La interoperabilidad no es crítica en etapas tempranas del proyecto y puede evolucionar posteriormente.|
|Fiabilidad|1|Aunque el sistema debe funcionar correctamente, un fallo ocasional no representa un riesgo crítico como en sistemas médicos o financieros. El usuario puede reintentar acciones sin consecuencias graves. Por ello, se acepta sacrificar cierto nivel de robustez extrema en favor de otros atributos más relevantes.|

![Priorización de atributos de calidad](resources/priorizacion_atributos_calidad_mentorsoft.png)

## Escenarios

- **Escenario 1: Estudiante envía evaluación sin finalizar**

|Item|Descripción|
|---|---|
|Atributo de calidad|**Capacidad de interacción** - Protección contra errores de usuario|
|Fuente de estímulo|Estudiante|
|Estímulo|El estudiante intenta enviar una evaluación incompleta o selecciona respuestas accidentalmente antes de finalizar.|
|Artefacto|Módulo de evaluación|
|Entorno|Durante una evaluación|
|Respuesta|El sistema: <br> alerta sobre preguntas sin responder, <br> permite revisar respuestas antes de enviar, <br> guarda progreso automáticamente, <br> solicita confirmación final, <br> recupera sesión si ocurre cierre inesperado del navegador.|
|Métrica de respuesta|Recuperación de sesión exitosa en al menos 98% de interrupciones. <br> Impide el envío de cuestionarios incompletos el 100% de las veces. <br> Tiempo de recuperación menor a 5 segundos.|
|Riesgos o implicaciones|Pérdida de respuestas por desconexiones. <br> Duplicidad de envíos. <br> Inconsistencias de sesión. <br> Sobrecarga por persistencia continua.|
|Tácticas arquitectónicas|Autosave incremental. <br> Persistencia temporal desacoplada. <br> Session recovery. <br> Caché temporal distribuida|

- **Escenario 2: Modificación de reglas de evaluación académica**

|Item|Descripción|
|---|---|
|Atributo de calidad|**Mantenibilidad** - Capacidad para ser modificado|
|Fuente de estímulo|Docente|
|Estímulo|El docente desea agregar un nuevo campo a la rubrica de calificación|
|Artefacto|Módulo de evaluación|
|Entorno|Un día cualquiera en el ambiente productivo|
|Respuesta|El sistema: <br> centraliza reglas académicas en un único componente, <br> minimiza impacto en evaluaciones existentes, <br> mantiene trazabilidad de cambios.|
|Métrica de respuesta|Cambio implementado en menos de 2 días. <br> Menos de 3 módulos afectados. <br> Cobertura automatizada superior al 80% sobre reglas modificadas.|
|Riesgos o implicaciones|Alta propagación de cambios. <br> Riesgo de inconsistencias históricas. <br> Dependencia excesiva del equipo técnico.|
|Tácticas arquitectónicas|Versionamiento de reglas. <br> APIs desacopladas|

- **Escenario 3: Protección contra modificación no autorizada de resultados de evaluación**

|Item|Descripción|
|---|---|
|Atributo de calidad|**Seguridad** - Integridad|
|Fuente de estímulo|Usuario malicioso autenticado o atacante interno.|
|Estímulo|Un usuario intenta alterar calificaciones, resultados diagnósticos o rutas de aprendizaje directamente desde solicitudes manipuladas o acceso indebido a la base de datos|
|Artefacto|Módulo de evaluación y módulo de clasificación|
|Entorno|Sistema en operación normal con múltiples usuarios concurrentes.|
|Respuesta|El sistema: <br> rechaza modificaciones no autorizadas, <br> valida permisos y contexto de operación, <br> registra auditoría inmutable de cambios, <br> detecta inconsistencias de integridad, <br> preserva trazabilidad histórica de resultados.|
|Métrica de respuesta|100% de modificaciones no autorizadas bloqueadas. <br> Trazabilidad completa de cambios críticos. <br> Detección de inconsistencias menor a 5 segundos. <br> Cero pérdida de integridad en resultados persistidos.|
|Riesgos o implicaciones|Escalamiento indebido de privilegios. <br> Manipulación directa de endpoints. <br> Alteración de datos desde accesos internos. <br> Falta de trazabilidad de cambios. <br> Inconsistencias entre servicios distribuidos.|
|Tácticas arquitectónicas|RBAC y políticas de autorización centralizadas. <br> Auditoría inmutable. <br> Validación server-side obligatoria. <br> Control transaccional ACID. <br> Principio de menor privilegio.|

- **Escenario 4: Respuesta rápida durante evaluaciones en línea**

|Item|Descripción|
|---|---|
|Atributo de calidad|**Eficiencia en desempeño** - Comportamiento temporal|
|Fuente de estímulo|Estudiantes concurrentes|
|Estímulo|Miles de estudiantes responden preguntas simultáneamente de la evaluación|
|Artefacto|Módulo de evaluación|
|Entorno|Pico de carga académica durante evaluaciones masivas|
|Respuesta|El sistema: <br> registra respuestas sin pérdida, <br> mantiene tiempos de respuesta bajos, <br> evita bloqueos de sesión, <br> preserva continuidad de la evaluación.|
|Métrica de respuesta|Tiempo de respuesta menor a 2 segundo por interacción. <br> Soporte de al menos 1.000 estudiantes concurrentes. <br> Pérdida de respuestas igual a 0. <br> Disponibilidad superior al 99.9% durante evaluación.|
|Riesgos o implicaciones|Saturación de base de datos. <br> Contención de escritura concurrente. <br> Timeouts de sesión. <br> Sobrecarga de red. <br> Bloqueos transaccionales. <br> Caída parcial del sistema durante picos|
|Tácticas arquitectónicas|Escalamiento horizontal. <br> Escritura asíncrona controlada. <br> Uso de colas de mensajería. <br> CQRS.|

## Justificación de tácticas arquitectónicas

|Escenario|Táctica|Justificación|Ventajas|Riesgos|
|---|---|---|---|---|
|1|Autosave incremental|Reduce la pérdida de respuestas guardando cambios parciales periódicamente durante la evaluación.|Minimiza pérdida de información, mejora experiencia del estudiante, permite recuperación rápida.|Sobrecarga de escrituras, aumento de tráfico de red, posibles inconsistencias si no hay control de concurrencia.
|1|Persistencia temporal desacoplada|Separa el almacenamiento temporal del definitivo para evitar afectar el rendimiento y la consistencia de las evaluaciones oficiales.|Mayor escalabilidad, aislamiento de fallos, mejor rendimiento en escritura temporal.|Complejidad arquitectónica adicional, necesidad de sincronización entre almacenamiento temporal y definitivo.|
|1|Session recovery|Permite restaurar el estado de la evaluación tras cierres inesperados o desconexiones.|Continuidad de la evaluación, reducción de frustración del usuario, mayor confiabilidad percibida.|Manejo complejo de sesiones, riesgo de sesiones inconsistentes o concurrentes.|
|1|Caché temporal distribuida|Almacena temporalmente el estado de evaluaciones activas para reducir latencia y carga sobre la base de datos principal.|Respuesta rápida, soporte de alta concurrencia, disminución de carga en persistencia principal.|Riesgo de pérdida temporal de datos si la caché falla, complejidad de sincronización y expiración de datos.|
|2|Versionamiento de reglas|Permite modificar la estructura de rúbricas sin afectar evaluaciones históricas ni reglas ya utilizadas en producción.|Conserva trazabilidad, facilita rollback, reduce inconsistencias históricas.|Incremento de complejidad en almacenamiento y mantenimiento de múltiples versiones.|
|2|APIs desacopladas|Evita que cambios en la estructura interna de rúbricas impacten directamente otros módulos consumidores.|Reduce propagación de cambios, facilita evolución independiente de componentes, mejora mantenibilidad.|Riesgo de sobreabstracción, necesidad de controlar compatibilidad entre versiones de APIs.|
|3|RBAC y políticas de autorización centralizadas|Controla qué usuarios pueden modificar calificaciones o resultados, evitando accesos indebidos y escalamiento de privilegios.|Centraliza reglas de seguridad, facilita auditoría y administración de permisos, reduce accesos no autorizados.|Configuración compleja de roles y riesgo de permisos mal definidos.|
|3|Auditoría inmutable|Permite registrar toda modificación crítica de forma trazable e irreversible para detectar manipulaciones o accesos indebidos.|Trazabilidad completa, soporte forense, detección de actividades sospechosas.|Incremento en almacenamiento y posible impacto en rendimiento si no se optimiza.|
|3|Validación server-side obligatoria|Evita confiar en datos manipulados desde el cliente y asegura que todas las reglas de negocio se validen en backend.|Reduce manipulación de endpoints, garantiza integridad de datos, fortalece seguridad.|Mayor carga de procesamiento en backend y duplicación parcial de validaciones frontend/backend.|
|3|Control transaccional ACID|Garantiza consistencia e integridad de los datos durante operaciones críticas concurrentes.|Evita corrupción de datos, asegura atomicidad y consistencia entre operaciones relacionadas.|Posibles bloqueos y degradación de rendimiento bajo alta concurrencia.|
|3|Principio de menor privilegio|Limita los permisos de usuarios y servicios únicamente a las operaciones necesarias.|Reduce superficie de ataque y minimiza impacto de cuentas comprometidas.|Mayor complejidad administrativa y riesgo de afectar funcionalidades por permisos insuficientes.|
|4|Escalamiento horizontal|Permite distribuir la carga de miles de estudiantes concurrentes entre múltiples instancias del módulo de evaluación, evitando saturación de un único nodo.|Alta disponibilidad, mejor manejo de picos de carga, mejora de latencia bajo concurrencia.|Complejidad en balanceo de carga, problemas de consistencia de sesión si no se diseña correctamente.|
|4|Escritura asíncrona controlada|Desacopla la recepción de respuestas de su persistencia inmediata, reduciendo bloqueo en la experiencia del estudiante.|Menor latencia percibida, mayor throughput, evita bloqueos por escritura directa en base de datos.|Riesgo de pérdida temporal de datos si falla la cola, consistencia eventual más compleja de gestionar.|
|4|Uso de colas de mensajería|Introduce un buffer entre la captura de respuestas y su procesamiento/persistencia para absorber picos de carga.|Manejo eficiente de picos, desacoplamiento entre componentes, mayor resiliencia ante fallos.|Latencia en procesamiento, complejidad operativa, necesidad de monitoreo de colas.|
|4|CQRS|Separa operaciones de lectura (visualización de preguntas) y escritura (envío de respuestas), optimizando cada flujo de forma independiente.|Optimización de rendimiento, escalabilidad independiente de lectura/escritura, reduce contención en base de datos.|Mayor complejidad arquitectónica, duplicación de modelos, consistencia eventual entre lecturas y escrituras.|

## Diagrama C4 nivel 3

![Diagrama C4 nivel 3](resources/c4_itmentorsoft_componentes.drawio.png)
