# Estratégia XMAN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia X MAN recria a lógica central do MetaTrader consultor especialista `X_MAN.mq4` dentro do StockSharp API de alto nível. O sistema negocia rompimentos impulsionados por uma média móvel ponderada linear rápida e lenta (LWMA), enquanto filtra entradas com impulso de vários períodos de tempo e uma confirmação mensal MACD. Ele foi projetado para negociações de continuação de tendência que são acionadas somente quando o impulso e a estrutura da tendência se alinham.

## Lógica de negociação

1. **Filtro de tendência principal** – Dois LWMAs calculados no período primário selecionado devem ser separados por pelo menos o `DistancePoints` configurável. Uma configuração longa exige que o LWMA rápido esteja acima do LWMA lento nessa margem, enquanto uma configuração curta precisa que o LWMA lento domine.
2. **Confirmação de Momentum** – A estratégia assina uma série de velas de prazo mais alto e a alimenta em um indicador de momentum. A distância absoluta das últimas três leituras de impulso do valor neutro (100) deve exceder o limite de compra ou venda correspondente pelo menos uma vez para permitir a negociação nessa direção.
3. **MACD Filtro** – Uma série de velas mensais gera um padrão (12, 26, 9) MACD. As negociações longas são permitidas apenas quando a linha MACD está acima da linha de sinal, e as negociações curtas exigem a relação oposta.
4. **Execução de Ordens** – Quando todos os filtros concordam, a estratégia entra usando ordens de mercado. As posições serão invertidas somente se a configuração oposta aparecer e a posição atual for plana ou na direção oposta.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Prazo primário usado para os cálculos LWMA. |
| `HigherCandleType` | Prazo maior alimentando o filtro de momentum. |
| `MacdCandleType` | Prazo para confirmação de MACD (mensal por padrão). |
| `FastMaPeriod` | Comprimento do LWMA rápido. |
| `SlowMaPeriod` | Comprimento do LWMA lento. |
| `MomentumPeriod` | Janela de lookback do oscilador de momento. |
| `MomentumBuyThreshold` | Distância mínima de 100 necessária para impulso de alta. |
| `MomentumSellThreshold` | Distância mínima de 100 necessária para impulso de baixa. |
| `DistancePoints` | Separação mínima entre o LWMA rápido e lento, expressa em faixas de preço. |
| `TakeProfitPoints` | Distância protetora opcional de lucro em pontos. |
| `StopLossPoints` | Distância de stop loss de proteção opcional em pontos. |

Todos os parâmetros são expostos por meio do `StrategyParam<T>` para que possam ser otimizados no StockSharp Designer ou configurados em tempo de execução.

## Gestão de risco

Se `TakeProfitPoints` ou `StopLossPoints` for maior que zero, a estratégia ativa o módulo de proteção integrado de StockSharp usando saídas de mercado. Nenhuma lógica adicional de rastreamento ou ponto de equilíbrio do especialista original MQL foi implementada ainda.

## Diferenças do especialista original

- A implementação do MetaTrader lidou com paradas de capital, movimentos de equilíbrio e opções complexas de gerenciamento de dinheiro. Esta conversão concentra-se nos principais filtros direcionais e entradas de mercado; a gestão de dinheiro em nível de carteira é intencionalmente omitida.
- O dimensionamento do pedido é delegado ao ambiente de hospedagem. A lógica original do lote-expoente não é reproduzida.
- Alertas, notificações por e-mail e modificações manuais de trailing stop não estão incluídos.

Essas mudanças mantêm a estratégia concisa e aproveitam o API de alto nível de API, preservando o conceito de negociação principal.
