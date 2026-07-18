# Estrategia de Ondas Heiken Ashi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina velas Heikin-Ashi con un filtro de onda de doble media móvil. El cruce de la SMA rápida (2) sobre la SMA lenta (30) señala posibles cambios de onda y se confirma con la dirección de la vela Heikin-Ashi actual.

## Detalles

- **Criterios de entrada**:
  - Largo: vela Heikin-Ashi alcista y SMA rápida cruzando por encima de la SMA lenta
  - Corto: vela Heikin-Ashi bajista y SMA rápida cruzando por debajo de la SMA lenta
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cruce opuesto
  - Trailing stop loss
- **Stops**: Trailing stop en puntos mediante `StopLoss`
- **Valores predeterminados**:
  - `FastLength` = 2
  - `SlowLength` = 30
  - `StopLoss` = new Unit(20, UnitTypes.Point)
  - `UseTrailing` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Heikin Ashi, SMA
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
