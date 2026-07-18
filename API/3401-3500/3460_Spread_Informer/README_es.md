# Estrategia de difusión del informador
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Recopila estadísticas detalladas para el diferencial de oferta y demanda del instrumento seleccionado y notifica cuando el diferencial supera un límite configurable. La estrategia escucha continuamente las actualizaciones de Nivel 1, rastrea la dispersión máxima, mínima y promedio en puntos y registra un resumen una vez que se detiene. Es útil para investigar las condiciones de liquidez antes de ejecutar sistemas sensibles a la latencia u optimizar las ventanas de negociación en el Probador de estrategias.

## Detalles

- **Fuente de datos**: cotizaciones de mejor oferta y mejor demanda de nivel 1.
- **Estadísticas capturadas**:
  - Marcas de tiempo de inicio y finalización del período de observación.
  - Difusión máxima y hora en la que ocurrió.
  - Spread mínimo y hora en que ocurrió.
  - Spread promedio calculado en todas las muestras de Nivel 1 observadas.
- **Alertas**:
  - Alerta opcional cuando el diferencial (en puntos) supera el umbral configurado `MaxSpreadPoints`.
  - La frecuencia de las alertas está limitada por `AlertIntervalSeconds` para evitar enviar spam al registro.
  - Las alertas sólo se activan cuando el diferencial cruza el umbral desde abajo.
- **Registro**:
  - Las alertas en tiempo real se escriben a través de `LogInfo`.
  - El resumen de estadísticas finales se emite durante `OnStopped`.
- **Valores predeterminados**:
  - `MaxSpreadPoints` = 0 (alertas deshabilitadas).
  - `AlertIntervalSeconds` = 0 (sin limitación).

## Parámetros

| Nombre | Descripción | Predeterminado | Notas |
| --- | --- | --- | --- |
| `MaxSpreadPoints` | Spread máximo permitido en puntos. Establezca en 0 para desactivar las alertas. | 0 | Los puntos se calculan utilizando el paso del precio del instrumento. |
| `AlertIntervalSeconds` | Tiempo mínimo entre alertas consecutivas. | 0 | Evita alertas duplicadas cuando la propagación sigue siendo amplia. |

## Notas de uso

1. Adjunte la estrategia a un instrumento y asegúrese de que los datos de Nivel 1 estén disponibles.
2. Configure `MaxSpreadPoints` según la extensión aceptable para el instrumento.
3. Opcionalmente, aumente `AlertIntervalSeconds` para suprimir notificaciones repetidas durante períodos volátiles.
4. Detenga la estrategia para revisar las estadísticas registradas en la salida del terminal.
