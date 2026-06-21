# Estratégia RSI Trader V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa o Índice de Força Relativa (RSI) para identificar reversões após extremos de curto prazo. Um sinal de compra ocorre quando o RSI cruza acima do limiar de sobrevenda após permanecer abaixo dele por dois candles consecutivos. Um sinal de venda ocorre quando o RSI cruza abaixo do limiar de sobrecompra após permanecer acima dele por dois candles. A estratégia opcionalmente fecha uma posição oposta existente e opera apenas dentro de uma janela de tempo configurável.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `RSI > BuyPoint` e o RSI dos dois candles anteriores `< BuyPoint`.
  - **Vendido**: `RSI < SellPoint` e o RSI dos dois candles anteriores `> SellPoint`.
- **Critérios de saída**: Sinal oposto ou stop/take-profit de proteção.
- **Filtro de tempo**: Opera apenas quando a hora de abertura do candle está entre `StartHour` e `EndHour`.
- **Stops**: Take profit e stop loss fixos expressos em unidades de preço.
- **Parâmetros**:
  - `RsiPeriod` – período de cálculo do RSI.
  - `BuyPoint` – nível de sobrevenda para entradas compradas.
  - `SellPoint` – nível de sobrecompra para entradas vendidas.
  - `CloseOnOpposite` – fechar a posição atual quando aparecer um sinal oposto.
  - `StartHour` / `EndHour` – horas de negociação.
  - `TakeProfit` / `StopLoss` – níveis de proteção em preço.

Este exemplo demonstra um sistema minimalista de cruzamento RSI construído com a API de alto nível do StockSharp. Pode ser usado como modelo para experimentação adicional.
