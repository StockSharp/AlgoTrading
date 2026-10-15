# Estrategia de Rompimiento Momentum Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida de la estrategia MQL original. Opera rompimientos de las Bandas de Bollinger confirmados por EMA, MACD y RSI. La estrategia entra solo una vez por cada expansión de volatilidad y sigue el stop a lo largo de la banda media mientras usa un take profit fijo en pips.

## Detalles

- **Criterios de entrada**:
  - Largo: ancho de banda por encima de `BreakoutFactor`, MACD > 0, RSI > 50, EMA por encima de la banda media, cierre anterior por encima de la banda superior anterior
  - Corto: ancho de banda por encima de `BreakoutFactor`, MACD < 0, RSI < 50, EMA por debajo de la banda media, cierre anterior por debajo de la banda inferior anterior
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: el precio toca el stop dinámico de la banda media o alcanza el take profit
  - Corto: el precio toca el stop dinámico de la banda media o alcanza el take profit
- **Stops**: El nivel de stop es la banda media actual de Bollinger, actualizado cada vela
- **Take Profit**: Distancia fija especificada en pips
- **Valores predeterminados**:
  - `BollingerLength` = 18
  - `BollingerDeviation` = 2m
  - `BreakoutFactor` = 0.0015m
  - `TakeProfitPips` = 100
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, EMA, MACD, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
