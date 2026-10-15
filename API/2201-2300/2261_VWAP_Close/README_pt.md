# Estratégia de Fechamento VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia calcula uma Média Móvel Ponderada por Volume (VWMA) dos preços de fechamento. Quando a VWMA muda de direção, ela atua como sinal para entradas ou saídas potenciais:

- Se a VWMA estava caindo e vira para cima (forma um vale), a estratégia fecha qualquer posição vendida e pode abrir uma posição comprada.
- Se a VWMA estava subindo e vira para baixo (forma um pico), a estratégia fecha qualquer posição comprada e pode abrir uma posição vendida.

## Parâmetros
- **Period** – número de velas usadas para o cálculo da VWMA.
- **Candle Type** – período das velas processadas.
- **Buy Open** – habilitar a abertura de posições compradas.
- **Sell Open** – habilitar a abertura de posições vendidas.
- **Buy Close** – permitir o fechamento de posições compradas quando a VWMA vira para baixo.
- **Sell Close** – permitir o fechamento de posições vendidas quando a VWMA vira para cima.

## Notas
A estratégia usa o indicador `VolumeWeightedMovingAverage` do StockSharp e processa apenas velas completadas. O volume da operação é retirado da propriedade `Volume` da estratégia; ao abrir uma nova posição, qualquer posição oposta é fechada automaticamente.
