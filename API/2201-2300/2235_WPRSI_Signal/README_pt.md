# Estratégia de Sinal WPRSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o especialista WPRSIsignal do MetaTrader. Combina o Williams Percent Range (WPR) e o Índice de Força Relativa (RSI) para gerar sinais de compra e venda.

## Lógica
- Um sinal de **compra** é gerado quando o WPR cruza acima de -20 a partir de baixo e o RSI está acima de 50. O sinal é confirmado apenas se o WPR permanecer acima de -20 pelas próximas `FilterUp` barras.
- Um sinal de **venda** é gerado quando o WPR cruza abaixo de -80 a partir de cima e o RSI está abaixo de 50. O sinal é confirmado apenas se o WPR permanecer abaixo de -80 pelas próximas `FilterDown` barras.
- Quando um sinal de compra é confirmado, a estratégia abre uma posição comprada se nenhuma estiver ativa. Quando um sinal de venda é confirmado, abre uma posição vendida se nenhuma estiver ativa.

## Parâmetros
- `Period` – comprimento de cálculo para WPR e RSI.
- `FilterUp` – número de barras que devem manter o WPR acima de -20 para confirmar um sinal de compra.
- `FilterDown` – número de barras que devem manter o WPR abaixo de -80 para confirmar um sinal de venda.
- `CandleType` – período de tempo das velas usadas para os cálculos.

## Uso
Anexe a estratégia a qualquer ativo. A estratégia usa `SubscribeCandles` e `Bind` para receber dados de velas e valores de indicadores. As posições são gerenciadas com ordens a mercado: `BuyMarket` para entradas compradas e `SellMarket` para entradas vendidas. A estratégia não implementa stop-loss ou take-profit; as posições são fechadas por sinais opostos.
