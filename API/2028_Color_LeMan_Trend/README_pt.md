# Estratégia de Tendência Color LeMan
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port do consultor especialista original MQL5 *ColorLeManTrend*. Utiliza um indicador de tendência personalizado baseado em máximos e mínimos para identificar a direção do mercado.

## Ideia

O indicador calcula linhas de alta e de baixa usando valores extremos de máximos e mínimos ao longo de três períodos de retrocesso diferentes. Médias móveis exponenciais suavizam esses valores. As decisões de trading são baseadas em cruzamentos das linhas de alta e de baixa:

- Quando a linha de alta anterior está acima da linha de baixa e a linha de alta atual cai abaixo da linha de baixa, um sinal de **compra** é gerado.
- Quando a linha de alta anterior está abaixo da linha de baixa e a linha de alta atual sobe acima da linha de baixa, um sinal de **venda** é gerado.
- Indicadores opcionais controlam se posições compradas ou vendidas podem ser abertas ou fechadas.

## Parâmetros

- `CandleType` – período para os cálculos do indicador.
- `Min` – período para o cálculo do extremo mais curto.
- `Midle` – período para o cálculo do extremo médio.
- `Max` – período para o cálculo do extremo mais longo.
- `PeriodEma` – período de suavização para as linhas de alta e de baixa.
- `StopLossPoints` – stop de proteção em pontos.
- `TakeProfitPoints` – take profit em pontos.
- `AllowBuy` – habilitar entradas compradas.
- `AllowSell` – habilitar entradas vendidas.
- `AllowBuyClose` – permitir o fechamento de posições compradas.
- `AllowSellClose` – permitir o fechamento de posições vendidas.
- `Volume` – volume de trading por ordem.

## Notas

A estratégia processa apenas candles finalizados e usa ordens a mercado para todas as operações. Os valores de stop-loss e take-profit são aplicados usando a proteção de posição integrada.
