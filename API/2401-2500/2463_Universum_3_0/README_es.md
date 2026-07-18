# Estrategia Universum 3.0
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el oscilador DeMarker que abre posiciones en cada barra completada y ajusta el volumen mediante un esquema martingala.

## Detalles

- **Criterios de entrada**:
  - Largo: `DeMarker > 0.5`
  - Corto: `DeMarker < 0.5`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Las posiciones se cierran por take profit o stop loss
- **Stops**: Puntos absolutos mediante `TakeProfitPoints` y `StopLossPoints`
- **Valores predeterminados**:
  - `DemarkerPeriod` = 10
  - `TakeProfitPoints` = 50m
  - `StopLossPoints` = 50m
  - `InitialVolume` = 1m
  - `LossesLimit` = 100
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo/Corto
  - Indicadores: DeMarker
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto
