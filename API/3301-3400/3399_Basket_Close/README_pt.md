# Estratégia Basket Close Utility
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Basket Close Utility reflete o comportamento do especialista MetaTrader "Basket Close 2". Ele monitora continuamente os lucros e perdas flutuantes de cada posição aberta na carteira conectada. When either a configurable profit objective or a loss limit is reached, the strategy sends market orders to flatten **all** exposures in every instrument involved. Opcionalmente, ele pode abrir automaticamente uma pequena posição de teste sempre que o livro estiver plano, o que é útil em backtests para validar se a lógica de proteção funciona conforme o esperado.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `LossMode` | Chooses whether the loss guard compares percentages or currency values. |
| `LossPercentage` | Negative percentage drawdown (expressed in absolute value) that triggers the loss exit when `LossMode` is `Percentage`. |
| `LossCurrency` | Floating loss in account currency that triggers the exit when `LossMode` is `Currency`. |
| `ProfitMode` | Escolhe se o objetivo de lucro compara porcentagens ou valores monetários. |
| `ProfitPercentage` | Percentage gain that closes all positions when `ProfitMode` is `Percentage`. |
| `ProfitCurrency` | Lucro flutuante na moeda da conta que fecha todas as posições quando `ProfitMode` é `Currency`. |
| `CandleType` | Prazo usado para acionar verificações periódicas dos lucros e perdas flutuantes. |
| `EnableTestOrders` | Quando habilitada, a estratégia envia uma única ordem de compra de mercado sempre que não houver posições abertas. |
| `TestOrderVolume` | Tamanho da negociação usado quando a ordem de teste opcional está ativa. |

## Lógica de negociação
1. Subscribe to the configured candle series and run the evaluation only when a candle is fully finished, matching the behaviour of the original EA that works on closed bars.
2. Agregue os lucros e perdas flutuantes de cada posição aberta. If the portfolio object exposes a combined floating profit it is used; otherwise the strategy sums the PnL of each position.
3. Compute the percentage change relative to the current account balance captured at start-up.
4. Acione a rotina de perda quando o PnL flutuante ultrapassar o limite configurado. Trigger the profit routine when the floating PnL or the percentage gain reaches the profit target.
5. Uma vez acionado, continue enviando ordens de mercado até que todas as posições abertas em todo o portfólio sejam fechadas. Isso inclui o título principal, bem como as posições abertas por estratégias infantis.
6. Opcionalmente, envie uma ordem de mercado para reabrir a exposição (para teste) depois que o livro ficar estável.

## Notas
- O especialista MetaTrader exibiu informações textuais no gráfico. Em StockSharp, os números importantes são registrados por meio de `LogInfo`.
- Os ajustes de swap e comissão do script original são implicitamente incluídos no PnL flutuante reportado pela carteira ou posições individuais.
- Os limites percentuais utilizam o saldo da conta capturado quando a estratégia é iniciada. Ajuste os limites ao realizar sessões longas se a base de capital mudar substancialmente.
- Quando a ordem de teste opcional está habilitada, a ordem auxiliar é reemitida sempre que a exposição anterior tiver sido encerrada pela guarda de lucros ou perdas.
