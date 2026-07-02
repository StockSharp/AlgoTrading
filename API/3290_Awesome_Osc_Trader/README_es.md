# Estrategia Awesome Osc Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el experto de MetaTrader "Awesome Osc Trader" combinando anchura de Bollinger Band, un filtro stochastic y una comprobación normalizada de momentum Awesome Oscillator. Las operaciones largas se abren cuando el oscilador sube desde un extremo negativo, mientras stochastic sale de la zona de sobreventa y la volatilidad del mercado permanece dentro de una anchura de banda configurable. Los cortos requieren condiciones reflejadas. Una ventana de trading configurable limita las órdenes nuevas a horas específicas, y las posiciones abiertas solo pueden cerrarse forzosamente por señales opuestas si la ganancia flotante coincide con el filtro elegido.

## Detalles

- **Criterios de entrada**:
  - El spread de Bollinger Band, convertido a pips, debe permanecer entre `BollingerSpreadLowerLimit` y `BollingerSpreadUpperLimit`.
  - La línea principal stochastic está por encima de `StochLower` para largos o por debajo de `StochUpper` para cortos.
  - Awesome Oscillator normalizado ha mostrado al menos cuatro barras consecutivas en el lado opuesto de cero y vuelve hacia cero con fuerza superior a `AoStrengthLimit`.
  - La hora actual está dentro de la ventana de trading definida por `EntryHour` y `OpenHours`.
- **Largo/Corto**: opera en ambas direcciones.
- **Criterios de salida**:
  - Salida temprana opcional cuando aparece una señal opuesta o cuando el oscilador cruza cero, controlada por `CloseTrade` y `ProfitTypeClTrd`.
  - Distancias de stop-loss, take-profit y trailing stop de protección suministradas en pips.
- **Stops**: stop fijo, take-profit y trailing stop opcional gestionados mediante `StartProtection`.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20, `BollingerSigma` = 2
  - `BollingerSpreadLowerLimit` = 55, `BollingerSpreadUpperLimit` = 380
  - `PeriodFast` = 3, `PeriodSlow` = 32
  - `AoStrengthLimit` = 0.13
  - `StochK` = 8, `StochD` = 3, `StochSlow` = 3
  - `StochLower` = 18, `StochUpper` = 76
  - `EntryHour` = 0, `OpenHours` = 16
  - `Lots` = 0.01, `TakeProfit` = 200, `StopLoss` = 80, `TrailingStop` = 40
  - `CloseTrade` = true, `ProfitTypeClTrd` = 1 (cerrar solo posiciones rentables)
- **Filtros**:
  - Categoría: Momentum con filtro de volatilidad
  - Dirección: Largo y corto
  - Indicadores: Bollinger Bands, Stochastic Oscillator, Awesome Oscillator
  - Stops: Sí (fijo y trailing)
  - Complejidad: Medio
  - Marco temporal: Diseñada para H1, pero funciona con cualquier serie de velas
  - Estacionalidad: Ventana de horas de trading
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
