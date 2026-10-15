# Estrategia Parabolic SAR Bug
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Parabolic SAR Bug** opera reversiones de tendencia utilizando el indicador Parabolic SAR. Cuando el SAR gira por debajo del precio, la estrategia entra largo, y cuando el SAR gira por encima del precio entra corto. El modo reverse opcional invierte las señales. El stop loss protector, take profit y trailing stop son compatibles a través del módulo de protección de posición integrado.

## Detalles

- **Criterios de entrada**: Cambio de dirección del Parabolic SAR.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal SAR opuesta o stop protector.
- **Stops**: Stop loss, take profit, trailing stop opcional.
- **Valores predeterminados**:
  - `Step` = 0.02
  - `MaxStep` = 0.2
  - `StopLossPercent` = 2
  - `TakeProfitPercent` = 1
  - `UseTrailingStop` = false
  - `Reverse` = false
  - `CloseOnSar` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Stop loss, take profit
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
