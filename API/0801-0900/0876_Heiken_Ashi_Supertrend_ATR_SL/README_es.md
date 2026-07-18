# Estrategia Heiken Ashi Supertrend ATR-SL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina velas Heikin Ashi con un filtro de dirección Supertrend. Las entradas requieren velas sin sombras y permiten habilitar un stop loss basado en ATR y punto de equilibrio.

## Detalles

- **Criterios de entrada**:
  - Largo: vela HA verde sin sombra inferior, filtro de tendencia alcista opcional
  - Corto: vela HA roja sin sombra superior, filtro de tendencia bajista opcional
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: vela HA roja sin sombra superior o stop alcanzado
  - Corto: vela HA verde sin sombra inferior o stop alcanzado
- **Stops**: Basado en ATR con punto de equilibrio opcional
- **Valores predeterminados**:
  - `UseSupertrend` = true
  - `AtrPeriod` = 10
  - `AtrFactor` = 3m
  - `UseBreakEven` = false
  - `BreakEvenAtrMultiplier` = 1m
  - `UseHardStop` = false
  - `StopLossAtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, Supertrend, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
