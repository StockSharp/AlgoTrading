# Estrategia Symr de Nueva Barra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Symr de Nueva Barra** demuestra cómo detectar el comienzo de nuevas velas en múltiples marcos temporales usando una única suscripción. La estrategia monitorea un marco temporal base y calcula cuándo comienzan intervalos más grandes como 5m, 15m, 30m, 1h, 4h, 1d, 20m y 55m. Cada barra detectada se registra en el log.

## Detalles

- **Criterios de entrada**: Ninguno. La estrategia no coloca operaciones.
- **Criterios de salida**: Ninguno.
- **Largo/Corto**: No aplica.
- **Stops**: No se utilizan stops.

### Parámetros

| Nombre | Predeterminado | Descripción |
|--------|----------------|-------------|
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Marco temporal base para la detección de nuevas barras. |

### Notas

- Almacena el último tiempo de apertura para cada período predefinido.
- Cuando el período base avanza, se evalúan y registran los períodos más grandes si se reinician.
- Útil como plantilla para el manejo de eventos en múltiples marcos temporales.
