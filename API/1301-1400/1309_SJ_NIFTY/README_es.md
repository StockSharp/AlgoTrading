# Estrategia SJ NIFTY
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que utiliza SuperTrend, VWAP, RSI y EMA200. La base del Canal Keltner actúa como filtro de tendencia opcional. El tamaño de posición se calcula a partir del porcentaje de riesgo del capital con stop-loss y take-profit basado en relación riesgo/beneficio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Cierre > SuperTrend && Cierre > VWAP && RSI > Sobrecomprado && Cierre > EMA200 && filtro base Keltner && Cierre > máximo anterior.
  - **Corto**: Cierre < SuperTrend && Cierre < VWAP && RSI < Sobrevendido && Cierre < EMA200 && filtro base Keltner && Cierre < mínimo anterior.
- **Criterios de salida**: Stop-loss o take-profit basado en relación de riesgo.
- **Tamaño de posición**: Porcentaje de riesgo del portafolio dividido por la distancia al stop, redondeado al tamaño del lote.
- **Indicadores**: SuperTrend, VWAP, RSI, EMA, Keltner Channels.
