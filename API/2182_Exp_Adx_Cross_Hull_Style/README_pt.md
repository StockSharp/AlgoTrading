# Estratégia Exp ADX Cross Hull Style
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina sinais de cruzamento do Average Directional Index (ADX) com um filtro de Hull Moving Average (HMA). As entradas ocorrem quando a linha +DI cruza acima da linha -DI para posições compradas ou abaixo para posições vendidas. As saídas são gerenciadas por um par de médias móveis Hull: a média rápida cruzando a média lenta fecha as posições. A estratégia opera no período de 4 horas por padrão.

## Detalhes
- **Critérios de entrada**  
  - **Comprado**: +DI cruza acima de -DI.  
  - **Vendido**: -DI cruza acima de +DI.
- **Critérios de saída**  
  - **Comprado**: HMA rápida cai abaixo da HMA lenta.  
  - **Vendido**: HMA rápida sobe acima da HMA lenta.
- **Indicadores**  
  - AverageDirectionalIndex (período 14).  
  - HullMovingAverage comprimento rápido 20.  
  - HullMovingAverage comprimento lento 50.
- **Período**: candles de 4 horas (configurável).
- **Stops**: nenhum por padrão.
- **Direção**: comprado e vendido.

A estratégia não depende de coleções históricas; reage a dados de candles em streaming. Os parâmetros podem ser otimizados para diferentes mercados. A saída do gráfico exibe candles de preço com ambas as médias móveis Hull e marcações de operações.
