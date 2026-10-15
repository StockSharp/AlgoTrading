# Estrategia de Disparador MA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina medias móviles exponenciales (EMA) rápidas y lentas con RSI para detectar reversiones de tendencia.
Cuando la EMA rápida y el RSI rápido están ambos por encima de sus contrapartes lentas, trata el mercado como alcista y abre una posición larga.
Cuando ambos están por debajo, abre una posición corta. Los parámetros permiten habilitar o deshabilitar entradas o salidas largas y cortas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA rápida > EMA lenta Y RSI rápido > RSI lento con tendencia previa bajista.
  - **Corto**: EMA rápida < EMA lenta Y RSI rápido < RSI lento con tendencia previa alcista.
- **Criterios de salida**:
  - **Largo**: la tendencia se vuelve bajista y se permiten salidas largas.
  - **Corto**: la tendencia se vuelve alcista y se permiten salidas cortas.
- **Indicadores**: EMA, RSI.
- **Stops**: No incluidos.
- **Marco temporal**: velas de 4 horas por defecto.
- **Parámetros**:
  - `FastRsiPeriod` = 3
  - `SlowRsiPeriod` = 13
  - `FastMaPeriod` = 5
  - `SlowMaPeriod` = 10
  - `AllowBuyEntry` = true
  - `AllowSellEntry` = true
  - `AllowLongExit` = true
  - `AllowShortExit` = true
