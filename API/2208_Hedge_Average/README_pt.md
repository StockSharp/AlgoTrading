# Estratégia Hedge Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o especialista "Hedge Average" do MetaTrader. Compara médias móveis simples dos preços de abertura e fechamento em dois períodos de tempo.

## Lógica de trading

- Calcular a SMA do preço de abertura e fechamento para `Period1` e `Period2`.
- Se a média de abertura do período longo estiver acima de sua média de fechamento **e** a média de abertura do período curto estiver abaixo de sua média de fechamento, uma posição comprada é aberta.
- Se a média de abertura do período longo estiver abaixo de sua média de fechamento **e** a média de abertura do período curto estiver acima de sua média de fechamento, uma posição vendida é aberta.
- O trading só é permitido entre `StartHour` e `EndHour`.
- Stop-loss e take-profit opcionais são definidos em unidades de preço absolutas. O trailing stop move o stop protetor junto com o preço quando habilitado.

## Parâmetros

- `Period1` – período para as médias rápidas.
- `Period2` – período para as médias lentas.
- `StartHour` – hora do dia em que o trading se torna ativo.
- `EndHour` – hora do dia em que o trading para.
- `CandleType` – período de vela usado para cálculos.
- `TakeProfit` – distância do take profit em unidades de preço.
- `StopLoss` – distância do stop loss em unidades de preço.
- `UseTrailing` – ativar trailing stop baseado na distância do stop-loss.

## Notas

A estratégia usa uma abordagem de posição única e não replica o objetivo de lucro baseado em dinheiro da versão MQL original.
