# Estrategia Elliott Wave con Salida por Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que entra en reversales similares a ZigZag y sale cuando cambia la dirección del Supertrend, con un stop-loss fijo en porcentaje.

## Detalles

- **Criterios de entrada**:
  - Largo: el precio forma un mínimo local
  - Corto: el precio forma un máximo local
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cambio de dirección del Supertrend o nivel de stop-loss
- **Stops**: Porcentaje fijo desde el precio de entrada
- **Valores predeterminados**:
  - `WaveLength` = 4
  - `SupertrendLength` = 10
  - `SupertrendMultiplier` = 3
  - `StopLossPercent` = 10
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Highest, Lowest, SuperTrend
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
