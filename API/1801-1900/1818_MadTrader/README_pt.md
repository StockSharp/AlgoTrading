# Estratégia Mad Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Mad Trader é uma estratégia de seguidor de tendência convertida do especialista MQL original "madtrader-8.7". Combina os indicadores ATR e RSI para identificar recuos de baixa volatilidade durante uma tendência emergente. O sistema aguarda que o ATR esteja abaixo de um limite especificado mas ainda em ascensão e que o RSI aumente dentro de uma tendência geral altista ou baixista. Quando essas condições se alinham e o corpo da vela está dentro dos limites definidos, a estratégia abre uma ordem a mercado na direção sugerida pelo RSI. As posições são protegidas por um stop de rastreamento e um mecanismo de lucro em cesta que fecha todas as operações assim que o patrimônio da conta atinge o crescimento alvo.

## Detalhes

- **Critérios de entrada**:
  - ATR está abaixo de `MaxAtr` e maior que o valor ATR anterior.
  - Tamanho do corpo da vela está entre `MinCandle` e `MaxCandle`.
  - Horário de trading está dentro de `[StartHour, EndHour)`.
  - Tendência RSI acima de 50 e RSI atual subindo mas abaixo de `RsiLowerLevel` → compra.
  - Tendência RSI abaixo de 50 e RSI atual caindo mas acima de `RsiUpperLevel` → venda.
  - Aplica um atraso mínimo de `TradeInterval` entre operações.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Stop de rastreamento atingido.
  - Meta de lucro em cesta atingida (`BasketProfit` ou `BasketProfit * BasketBoost`).
- **Stops**: Trailing medido em pontos de preço.
- **Valores padrão**:
  - `AtrPeriod` = 14
  - `RsiPeriod` = 14
  - `TrendBars` = 60
  - `MinCandle` = 5
  - `MaxCandle` = 10
  - `MaxAtr` = 10
  - `RsiUpperLevel` = 50
  - `RsiLowerLevel` = 50
  - `StartHour` = 0
  - `EndHour` = 23
  - `TradeInterval` = 30 minutos
  - `TrailingStop` = 7
  - `BasketProfit` = 1.05
  - `BasketBoost` = 1.1
  - `RefreshHours` = 24
  - `ExponentialGrowth` = 0.01
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: ATR, RSI
  - Stops: Trailing
  - Complexidade: Moderado
  - Período: Curto prazo (velas de 5 minutos)
  - Nível de risco: Médio
