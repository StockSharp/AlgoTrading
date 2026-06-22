# Estratégia de Escape
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base em médias móveis simples dos preços de abertura das velas. Ela compara o fechamento mais recente de 5 minutos com duas médias móveis calculadas sobre o preço de abertura:

- **SMA rápida (4 períodos)** – usada como limiar para entradas vendidas.
- **SMA lenta (5 períodos)** – usada como limiar para entradas compradas.

## Como funciona

1. Em cada vela de 5 minutos concluída, a estratégia atualiza duas SMAs do preço de abertura das velas.
2. Se não houver posição ativa:
   - Entrar **comprado** quando o último fechamento estiver abaixo da SMA lenta.
   - Entrar **vendido** quando o último fechamento estiver acima da SMA rápida.
3. Após entrar em uma posição, a estratégia define níveis fixos de stop-loss e take-profit medidos em unidades de preço.
4. A posição é fechada quando o take-profit ou stop-loss é atingido.

A lógica usa a API de alto nível do StockSharp e é destinada a fins educativos.

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `FastLength` | Período da SMA rápida. | `4` |
| `SlowLength` | Período da SMA lenta. | `5` |
| `TakeProfitLong` | Distância de take-profit para trades comprados em unidades de preço. | `25` |
| `TakeProfitShort` | Distância de take-profit para trades vendidos em unidades de preço. | `26` |
| `StopLossLong` | Distância de stop-loss para trades comprados em unidades de preço. | `25` |
| `StopLossShort` | Distância de stop-loss para trades vendidos em unidades de preço. | `3` |
| `CandleType` | Tipo de vela usado para análise. | `TimeFrame(5m)` |

Todos os parâmetros podem ser otimizados pelo otimizador do StockSharp.
