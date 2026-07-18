# Estrategia RSI Stochastic WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina RSI, Oscilador Estocástico y una Media Móvil Ponderada (WMA).
Compra cuando el RSI está sobrevendido, %K cruza por encima de %D y el precio está por encima de la WMA.
Vende en corto cuando el RSI está sobrecomprado, %K cruza por debajo de %D y el precio está por debajo de la WMA.

## Detalles

- **Criterios de entrada**:
  - Largo: `RSI < 30 && %K crosses above %D && Close > WMA`
  - Corto: `RSI > 70 && %K crosses below %D && Close < WMA`
- **Largo/Corto**: Ambos
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `StochK` = 14
  - `StochD` = 3
  - `WmaLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: RSI, Stochastic, WMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
