# Estratégia de Scalping Angry Bird
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o consultor especialista MetaTrader "Angry Bird (Scalping)" usando a API de alto nível do StockSharp.

## Lógica
- Observa velas de 15 minutos e calcula a máxima mais alta e a mínima mais baixa nas últimas barras `Depth` para derivar um passo de grade dinâmico.
- Quando não há posição aberta e a vela anterior fecha acima da atual, o RSI no período horário dispara entradas: valores acima de `RsiMin` abrem posições vendidas, valores abaixo de `RsiMax` abrem posições compradas.
- Se uma posição existe e o preço se move contra ela pelo menos o passo da grade, uma nova posição é aberta na mesma direção com seu volume multiplicado por `LotExponent` até que `MaxTrades` seja atingido.
- Uma leitura forte do CCI acima de `CciDrop` para vendidos ou abaixo de `-CciDrop` para comprados força o fechamento de todas as posições.
- As posições também são fechadas quando o lucro atinge `TakeProfit` ou a perda atinge `StopLoss` relativo ao preço médio de entrada.

## Parâmetros
- `StopLoss` – stop loss em pontos.
- `TakeProfit` – take profit em pontos.
- `DefaultPips` – distância mínima entre ordens de grade em pips.
- `Depth` – número de velas usadas para cálculo de máxima/mínima.
- `LotExponent` – multiplicador para o volume de ordens subsequentes.
- `MaxTrades` – número máximo de posições de média.
- `RsiMin` / `RsiMax` – limiares de RSI para entrada.
- `CciDrop` – valor absoluto do CCI que força o fechamento de posições.
- `Volume` – volume inicial da ordem.
- `CandleType` – período das velas de trabalho (padrão 15 minutos).

## Uso
Anexe a estratégia a um instrumento e inicie. A estratégia usa ordens a mercado e gerencia uma única posição líquida, fazendo média à medida que o preço se move contra ela.
