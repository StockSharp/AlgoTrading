# Estrategia Dual Supertrend MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Dual Supertrend MACD combina dos indicadores Supertrend con un filtro MACD.
Se abre una posición larga cuando el precio opera por encima de ambas líneas Supertrend y el histograma MACD es positivo.
Las posiciones cortas aparecen cuando el precio está por debajo de ambas líneas y el histograma es negativo.
Las posiciones se cierran una vez que cualquier Supertrend cambia de dirección o el histograma MACD cruza cero.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - Largo: `Close > Supertrend1 && Close > Supertrend2 && MACD Histogram > 0`
  - Corto: `Close < Supertrend1 && Close < Supertrend2 && MACD Histogram < 0`
- **Criterios de salida**:
  - Largo: `Close < Supertrend1 || Close < Supertrend2 || MACD Histogram < 0`
  - Corto: `Close > Supertrend1 || Close > Supertrend2 || MACD Histogram > 0`
- **Stops**: Ninguno por defecto.
- **Valores predeterminados**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `OscillatorMaType` = Exponential
  - `SignalMaType` = Exponential
  - `AtrPeriod1` = 10
  - `Factor1` = 3.0
  - `AtrPeriod2` = 20
  - `Factor2` = 5.0
  - `TradeDirection` = "Both"
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Configurable
  - Indicadores: Supertrend, MACD
  - Complejidad: Intermedio
  - Nivel de riesgo: Medio
