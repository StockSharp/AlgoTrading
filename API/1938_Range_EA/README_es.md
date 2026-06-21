# Estrategia Range EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera desviaciones de precio desde una media móvil dentro de un rango fijo. Abre posiciones largas o cortas cuando el precio se mueve una distancia especificada desde la media. Admite stop trailing opcional, promediación escalonada, módulo de reversión y filtro de sesión de trading.

## Detalles

- **Criterios de entrada**:
  - Largo: precio de cierre por encima de media móvil + `Range`
  - Corto: precio de cierre por debajo de media móvil - `Range`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Alcanzar `TakeProfit` o `StopLoss`
  - Stop trailing activo cuando está habilitado
  - Reversión opcional después de movimiento de `Turn`
- **Stops**: Valor fijo
- **Valores predeterminados**:
  - `MaLength` = 21
  - `Range` = 250m
  - `TakeProfit` = 500m
  - `StopLoss` = 250m
  - `UseTrailingStop` = true
  - `TrailingStop` = 250m
  - `UseTurn` = true
  - `Turn` = 250m
  - `LotMultiplicator` = 1.65m
  - `TurnTakeProfit` = 500m
  - `UseStepDown` = false
  - `StepDown` = 150m
  - `UseTradeTime` = false
  - `OpenTradeTime` = 08:00:00
  - `CloseTradeTime` = 21:30:00
  - `OrderVolume` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
