# Estratégia Figurelli Series
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia converte o especialista MetaTrader5 "Exp_FigurelliSeries" para o StockSharp. Ela usa um indicador personalizado Figurelli Series que mede a diferença entre o número de médias móveis acima e abaixo do preço atual. As negociações ocorrem uma vez por dia em um horário de início definido pelo usuário e todas as posições são fechadas em um horário de parada.

## Indicador
O indicador Figurelli Series cria uma cadeia de médias móveis exponenciais começando pelo *Start Period* e incrementando em *Step* para *Total* médias. Em cada barra, conta quantas médias estão acima e abaixo do preço de fechamento. O valor do indicador é `bids - asks` onde `bids` é a contagem de médias abaixo do preço e `asks` é a contagem de médias acima do preço.

## Regras de negociação
- Em `Start Hour:Start Minute`:
  - Comprar se o valor do indicador for positivo e não houver posição comprada.
  - Vender se o valor do indicador for negativo e não houver posição vendida.
- A partir de `Stop Hour:Stop Minute`, qualquer posição aberta é fechada.
- Apenas velas terminadas do `Candle Type` selecionado são usadas.

## Parâmetros
- `StartPeriod` – período inicial da média móvel.
- `Step` – incremento de período entre médias.
- `Total` – número de médias móveis.
- `StartHour` / `StartMinute` – hora em que as entradas podem ocorrer.
- `StopHour` / `StopMinute` – hora para fechar todas as posições.
- `CandleType` – tipo de vela para cálculos.
