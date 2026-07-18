# Estratégia de Ciclo de Tendência Color Schaff WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o especialista **Color Schaff WPR Trend Cycle** do MetaTrader.
Utiliza o Schaff Trend Cycle calculado a partir dos valores rápidos e lentos do Williams %R para detectar reversões de mercado.

O algoritmo trabalha apenas com candles finalizados. Quando o valor do indicador cruza acima do nível superior, a estratégia abre uma posição comprada e fecha qualquer posição vendida existente. Quando o valor cruza abaixo do nível inferior, abre uma posição vendida e fecha qualquer posição comprada existente.

## Parâmetros
- **Fast WPR** – período para o cálculo do Williams %R rápido.
- **Slow WPR** – período para o cálculo do Williams %R lento.
- **Cycle** – comprimento do ciclo utilizado no cálculo do Schaff Trend.
- **High Level** – nível de ativação superior para entradas compradas.
- **Low Level** – nível de ativação inferior para entradas vendidas.
- **Candle Type** – período das candles para avaliação do indicador.

## Links
- Fonte MQL original: `MQL/13489/mql5/Experts/exp_colorschaffwprtrendcycle.mq5`
- Indicador: `MQL/13489/mql5/Indicators/colorschaffwprtrendcycle.mq5`
