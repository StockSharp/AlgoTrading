# Estrategia de Tendencia XAUUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera XAUUSD usando cruces de EMA, extremos de RSI y Bollinger Bands.
Se abre una posición larga cuando la EMA rápida cruza por encima de la lenta, el RSI está por debajo del nivel de sobreventa y el precio cierra por encima de la banda superior de Bollinger.
Las posiciones cortas se abren en condiciones opuestas.
La gestión del riesgo establece niveles de stop-loss y take-profit basados en el porcentaje de riesgo de la cartera y una ratio take-profit/stop-loss.

## Detalles

- **Entrada**:
  - Largo: cruce ascendente de EMA rápida, RSI < oversold, close > banda superior.
  - Corto: cruce descendente de EMA rápida, RSI > overbought, close < banda inferior.
- **Salida**: stop-loss o take-profit calculados según los parámetros de riesgo.
- **Indicadores**: EMA, RSI, Bollinger Bands.
