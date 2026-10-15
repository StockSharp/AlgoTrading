# Estratégia Limits RSI Momentum Bot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
Esta estratégia coloca ordens limitadas com base nos indicadores de Índice de Força Relativa (RSI) e Momentum. Seu objetivo é comprar com descontos e vender com prêmios usando ordens pendentes em vez de execuções a mercado.

## Regras de Trading
- Opera apenas durante a janela de tempo especificada.
- Em cada vela finalizada, os valores de RSI e Momentum são calculados.
- Uma **ordem limitada de compra** é colocada abaixo da abertura da vela quando RSI e Momentum estão ambos abaixo de seus limiares de compra.
- Uma **ordem limitada de venda** é colocada acima da abertura da vela quando RSI e Momentum estão ambos acima de seus limiares de venda.
- Quando uma posição é aberta, a ordem pendente oposta é cancelada.
- Stop-loss e take-profit são gerenciados automaticamente via `StartProtection`.

## Parâmetros
- `Volume` – volume da ordem.
- `LimitOrderDistance` – distância em passos de preço da abertura da vela para colocar ordens pendentes.
- `TakeProfit` – objetivo de lucro em passos de preço.
- `StopLoss` – limite de perda em passos de preço.
- `RsiPeriod` – período para cálculo do RSI.
- `RsiBuyRestrict` / `RsiSellRestrict` – limiares de RSI que permitem entradas compradas ou vendidas.
- `MomentumPeriod` – período para cálculo do Momentum.
- `MomentumBuyRestrict` / `MomentumSellRestrict` – limiares de Momentum para entradas compradas ou vendidas.
- `StartTime` / `EndTime` – limites da sessão de trading.
- `CandleType` – intervalo de vela usado para cálculos de indicadores.

## Observações
A estratégia é convertida do script MQL4 "The Limits Bot with RSI & Momentum" e usa a API de alto nível do StockSharp.
