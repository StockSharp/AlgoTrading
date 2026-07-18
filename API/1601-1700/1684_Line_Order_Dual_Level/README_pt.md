# Estratégia de Ordem por Níveis Duplos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma adaptação do consultor especialista MetaTrader "MyLineOrder" para a API StockSharp. Permite ao trader definir níveis de preço horizontais que acionam ordens de mercado automáticas quando tocados. As distâncias opcionais de stop loss, take profit e trailing stop são expressas em pips, e o volume de negociação é configurável.

Quando o preço de mercado atinge o nível **BuyPrice**, a estratégia entra em uma posição comprada. Ao tocar o nível **SellPrice**, abre-se uma posição vendida. Após a entrada, a estratégia monitora a posição e sai quando uma das condições de proteção é atendida: stop loss, take profit ou trailing stop.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Preço toca ou supera `BuyPrice`.
  - **Vendido**: Preço toca ou cai abaixo de `SellPrice`.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop loss, take profit ou trailing stop.
- **Stops**:
  - `StopLossPips`, `TakeProfitPips`, `TrailingStopPips`.
- **Filtros**:
  - Nenhum.
- **Parâmetros**:
  - `BuyPrice` – nível para entrada comprada.
  - `SellPrice` – nível para entrada vendida.
  - `StopLossPips` – distância de stop loss em pips.
  - `TakeProfitPips` – distância de take profit em pips.
  - `TrailingStopPips` – distância de trailing stop em pips.
  - `TradeVolume` – volume da ordem.
  - `CandleType` – período dos candles para monitoramento do preço.
