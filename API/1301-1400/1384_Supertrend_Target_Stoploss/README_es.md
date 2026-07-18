# Estrategia Supertrend con Objetivo y Stop Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que compra cuando el precio cruza por encima de la línea Supertrend y vende cuando cruza por debajo. Un objetivo y un stop loss de porcentaje fijo cierran las posiciones.

## Detalles

- **Criterios de entrada**: Precio cruzando el Supertrend.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Porcentaje de objetivo o stop loss.
- **Stops**: Sí, porcentaje fijo.
- **Valores predeterminados**:
  - `Period` = 14
  - `Multiplier` = 3m
  - `TargetPct` = 0.01m
  - `StopPct` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, Supertrend
  - Stops: Fijo
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
