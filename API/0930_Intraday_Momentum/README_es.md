# Estrategia de Momentum Intradía
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera dentro de una sesión especificada usando cruce de EMA, filtro RSI y confirmación VWAP. Entra largo cuando la EMA rápida cruza por encima de la EMA lenta, el RSI está por debajo del nivel de sobrecompra y el precio está por encima del VWAP. Cortos en condiciones opuestas. Aplica porcentajes fijos de stop-loss y take-profit y cierra cualquier posición al final de la sesión.

## Parámetros

- **EmaFastLength**: Longitud de la EMA rápida.
- **EmaSlowLength**: Longitud de la EMA lenta.
- **RsiLength**: Período del RSI.
- **RsiOverbought**: Nivel de sobrecompra del RSI.
- **RsiOversold**: Nivel de sobreventa del RSI.
- **StopLossPerc**: Porcentaje de stop-loss.
- **TakeProfitPerc**: Porcentaje de take-profit.
- **StartHour**: Hora de inicio de la sesión.
- **StartMinute**: Minuto de inicio de la sesión.
- **EndHour**: Hora de fin de la sesión.
- **EndMinute**: Minuto de fin de la sesión.
- **CandleType**: Tipo de velas.

