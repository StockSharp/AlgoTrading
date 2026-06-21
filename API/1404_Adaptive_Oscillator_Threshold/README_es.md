# Estrategia de Umbral de Oscilador Adaptativo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Umbral de Oscilador Adaptativo utiliza el RSI con un umbral dinámico basado en el Umbral Adaptativo de Bufi (BAT). Compra cuando el RSI cae por debajo de un nivel fijo o un umbral adaptativo.

## Detalles

- **Criterios de entrada**: El RSI cae por debajo del umbral fijo o adaptativo
- **Largo/Corto**: Largo
- **Criterios de salida**: Salida por barras fijas o stop-loss en dólares
- **Stops**: Stop-loss en dólares
- **Valores predeterminados**:
  - `UseAdaptiveThreshold` = true
  - `RsiLength` = 2
  - `BuyLevel` = 14
  - `AdaptiveLength` = 8
  - `AdaptiveCoefficient` = 6
  - `ExitBars` = 28
  - `DollarStopLoss` = 1600
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Largo
  - Indicadores: RSI, StandardDeviation, LinearRegression
  - Stops: Dólar
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
