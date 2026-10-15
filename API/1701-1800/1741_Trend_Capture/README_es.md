# Captura de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que combina el Parabolic SAR con un filtro ADX. Las operaciones largas ocurren cuando el precio cierra por encima del valor SAR mientras el ADX permanece por debajo de un umbral, señalando una tendencia naciente. Las operaciones cortas se abren en la condición opuesta.

## Detalles

- **Criterios de entrada**: Precio por encima/debajo del Parabolic SAR con ADX por debajo de `AdxLevel`.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Stop loss, take profit o señal opuesta.
- **Stops**: Stop loss fijo, take profit y ajuste de break-even.
- **Valores predeterminados**:
  - `SarStep` = 0.02
  - `SarMax` = 0.2
  - `AdxPeriod` = 14
  - `AdxLevel` = 20
  - `StopLoss` = 1800 puntos
  - `TakeProfit` = 500 puntos
  - `BreakEven` = 50 puntos
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR, ADX
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
