# Estratégia RSI EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia emula um consultor especialista clássico de RSI. Opera quando o Índice de Força Relativa cruza níveis predefinidos e gerencia o risco com stop loss, take profit e trailing stop opcional.

## Lógica da estratégia
- Calcula o RSI usando o parâmetro configurável `RsiPeriod`.
- **Entrada comprada** quando o RSI sobe acima de `BuyLevel` e não existe posição comprada.
- **Entrada vendida** quando o RSI cai abaixo de `SellLevel` e não existe posição vendida.
- Quando `CloseBySignal` está habilitado, um cruzamento oposto fecha a posição existente.
- As posições podem ser protegidas com `StopLoss`, `TakeProfit` e `TrailingStop` medidos em unidades de preço.
- Funciona com dados de candles definidos por `CandleType`.

## Parâmetros
- `OpenBuy` – habilitar entradas compradas.
- `OpenSell` – habilitar entradas vendidas.
- `CloseBySignal` – fechar pelo sinal RSI oposto.
- `StopLoss` – perda em unidades de preço.
- `TakeProfit` – lucro em unidades de preço.
- `TrailingStop` – distância de trailing em unidades de preço.
- `RsiPeriod` – comprimento do cálculo do RSI.
- `BuyLevel` – limiar RSI para sinais comprados.
- `SellLevel` – limiar RSI para sinais vendidos.
- `CandleType` – período ou tipo de candle para assinar.

O volume de operação padrão é controlado pela propriedade `Volume` da estratégia.
