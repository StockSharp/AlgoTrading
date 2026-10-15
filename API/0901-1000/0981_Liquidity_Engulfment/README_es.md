# Estrategia de Engullimiento de Liquidez
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia detecta patrones de engullimiento alcistas y bajistas que ocurren cuando el precio toca máximos o mínimos recientes de liquidez. Las operaciones se filtran por modo e incluyen stop loss fijo y take profit opcional definidos en pips.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Engullimiento alcista tras tocar la liquidez inferior.
  - **Corto**: Engullimiento bajista tras tocar la liquidez superior.
- **Criterios de salida**: Señal opuesta, stop loss o take profit.
- **Largo/Corto**: Configurable (ambos por defecto).
- **Indicadores**: Highest, Lowest.
- **Stops**: `StopLossPips` y `TakeProfitPips` opcional.
- **Valores predeterminados**:
  - `CandleType` = 1 minuto
  - `UpperLookback` = 10
  - `LowerLookback` = 10
  - `StopLossPips` = 10
  - `TakeProfitPips` = 20
  - `Mode` = Both
