# Estrategia BabyShark VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina una banda de precio promedio ponderado por volumen (VWAP) con un filtro RSI basado en OBV. Las operaciones largas ocurren cuando el precio cae por debajo de la banda de desviación inferior y el RSI señala sobreventa. Las operaciones cortas se activan cuando el precio sube por encima de la banda superior y el RSI está sobrecomprado.

Los stops utilizan un pequeño porcentaje de pérdida y las posiciones esperan un período de enfriamiento antes de volver a entrar.

## Detalles

- **Criterios de entrada**: El precio cruza las bandas de desviación con confirmación del RSI.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Retorno al VWAP o stop-loss.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `Length` = 60
  - `RsiLength` = 5
  - `HigherLevel` = 70
  - `LowerLevel` = 30
  - `Cooldown` = 10
  - `StopLossPercent` = 0.6m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: VWAP, RSI, OBV
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
