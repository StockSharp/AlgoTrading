# Estratégia de Aleatoriedade RRS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Aleatoriedade RRS** é uma versão StockSharp da “RRS Aleatoriedade na Natureza EA” para MetaTrader 4.
Ela emula o Expert Advisor original com entradas de compra ou venda pseudoaleatórias, verificações de stop-loss e take-profit em candles concluídos, trailing opcional e liquidação quando a perda flutuante atinge o limite configurado.

Como StockSharp usa posições líquidas por título, a exposição simultânea comprada e vendida não é suportada. Assim, `DoubleSide` começa comprado e alterna a direção após cada entrada, em vez de manter duas negociações protegidas como no MetaTrader.

## Lógica de negociação

1. Em cada candle concluído, a estratégia usa o fechamento para as proteções e, quando disponíveis, bid/ask de Nível 1 para o spread e o preço de liquidação.
2. Com posição aberta, verifica stop loss, take profit, trailing stop e o limite de perda flutuante; envia no máximo uma ordem de fechamento por candle.
3. Quando estável, valida as restrições de spread e volume antes de abrir uma nova negociação:
   - **DoubleSide** alterna entre compra e venda, começando por compra.
   - **OneSide** usa um inteiro pseudoaleatório repetível em `[0,5]`: `1` ou `4` compra, `0` ou `3` vende e `2` ou `5` ignora o candle. A sequência reinicia ao iniciar ou resetar a estratégia.
4. Os volumes de negociação são desenhados uniformemente entre o mínimo e o máximo configurados e alinhados ao passo de volume do instrumento.

## Parâmetros

| Grupo | Nome | Descrição |
|-------|------|-------------|
| Geral | `Mode` | Entradas alternadas (`DoubleSide`, `0`) ou filtradas aleatoriamente (`OneSide`, `1`). |
| Configurações de lote | `MinVolume` / `MaxVolume` | Faixa de volume para negociações geradas aleatoriamente. |
| Proteção | `TakeProfitPoints` | Distância de lucro em etapas de preço. |
| Proteção | `StopLossPoints` | Distância de stop-loss em etapas de preço. |
| Proteção | `TrailingStartPoints` | Distância de lucro que permite o gerenciamento de trailing stop. |
| Proteção | `TrailingGapPoints` | Compensação entre o preço de mercado e o trailing stop. |
| Filtros | `MaxSpreadPoints` | Spread máximo de Nível 1 em passos de preço. Zero bloqueia novas entradas; valor positivo permite fallback por candles sem bid/ask. |
| Filtros | `SlippagePoints` | Configuração de deslizamento informativo (não aplicada automaticamente). |
| Gestão de Risco | `MoneyRiskMode` | Perda fixa (`FixedMoney`, `0`) ou percentual do portfólio (`BalancePercentage`, `1`). |
| Gestão de Risco | `RiskValue` | Quantidade de risco (moeda ou porcentagem dependendo da modalidade). |
| Geral | `TradeComment` | Comentário das entradas; ordens de fechamento acrescentam o motivo do acionamento. |
| Geral | `CandleType` | Série de velas conduzindo o ciclo de decisão. |

## Notas

- Cotações de Nível 1 melhoram o cálculo do spread e do preço de liquidação. Sem os dois lados, limite positivo permite fallback por candles; `MaxSpreadPoints = 0` sempre bloqueia entradas.
- As proteções são avaliadas em candles concluídos. O trailing ativa após `TrailingStartPoints + TrailingGapPoints` passos de lucro e acompanha o preço a `TrailingGapPoints`.
- `FixedMoney` interpreta `RiskValue` na moeda da conta; `BalancePercentage` usa esse percentual do valor atual do portfólio.
