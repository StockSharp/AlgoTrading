# Estrategia de Ciclo de Tendencia Color Schaff WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el experto **Color Schaff WPR Trend Cycle** de MetaTrader.
Utiliza el Schaff Trend Cycle calculado a partir de los valores rápidos y lentos de Williams %R para detectar giros del mercado.

El algoritmo trabaja únicamente con velas terminadas. Cuando el valor del indicador cruza por encima del nivel superior, la estrategia abre una posición larga y cierra cualquier posición corta existente. Cuando el valor cruza por debajo del nivel inferior, abre una posición corta y cierra cualquier posición larga existente.

## Parámetros
- **Fast WPR** – período para el cálculo del Williams %R rápido.
- **Slow WPR** – período para el cálculo del Williams %R lento.
- **Cycle** – longitud del ciclo utilizado en el cálculo del Schaff Trend.
- **High Level** – nivel de activación superior para entradas largas.
- **Low Level** – nivel de activación inferior para entradas cortas.
- **Candle Type** – marco temporal de las velas para la evaluación del indicador.

## Enlaces
- Fuente MQL original: `MQL/13489/mql5/Experts/exp_colorschaffwprtrendcycle.mq5`
- Indicador: `MQL/13489/mql5/Indicators/colorschaffwprtrendcycle.mq5`
