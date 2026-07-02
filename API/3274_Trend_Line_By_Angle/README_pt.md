# Estratégia de linha de tendência por ângulo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A estratégia é uma versão para StockSharp do expert advisor do MetaTrader *Trend Line By Angle*. O robô original misturava entradas manuais por botões com ferramentas extensas de gestão de dinheiro. Esta versão converte o fluxo discricionário em um sistema automatizado de seguimento de tendência por MACD, preservando a lógica de proteção:

- MACD mensal (12/26/9), calculado no tipo de candle de sinal configurado, define a direção. Cruzamentos altistas abrem exposição comprada; cruzamentos baixistas abrem exposição vendida.
- As entradas escalam até o número configurado de blocos, espelhando os cliques manuais repetidos no EA de origem.
- Bollinger Bands (20, 2) observam o período de execução. Tocar a banda superior liquida a exposição comprada; tocar a banda inferior liquida posições vendidas, replicando os botões visuais de stop do MetaTrader.
- Controles clássicos de risco - stop-loss, take-profit, trailing stop e movimento para break-even - operam sobre distâncias em pips convertidas pelo `PriceStep` do instrumento.
- A proteção em nível de conta fecha todas as ordens quando um alvo de lucro monetário ou percentual é atingido. Um bloqueio trailing adicional baseado em dinheiro acompanha o lucro flutuante e sai no drawdown configurado.

## Fluxo de execução

1. **Preparação dos indicadores** - `MovingAverageConvergenceDivergenceSignal` roda em `SignalCandleType`, enquanto `BollingerBands` rodam no `CandleType` de negociação.
2. **Sinais de entrada** - Em cada candle de execução concluído, o cruzamento MACD mais recente é avaliado. Um cruzamento para cima aciona `BuyMarket`; um cruzamento para baixo aciona `SellMarket`. A exposição oposta existente é fechada antes da reversão.
3. **Lógica de escalonamento** - A estratégia continua comprando/vendendo até que a posição agregada atinja `TradeVolume * MaxEntries`.
4. **Gestão de risco** - Níveis de break-even, trailing stop, stop-loss e take-profit são recalculados em cada candle. Um toque em Bollinger força uma saída mesmo que outros níveis não sejam atingidos.
5. **Proteção da conta** - Verificações de take-profit monetário e percentual rodam antes de gerar novos sinais. O módulo de trailing monetário acompanha o maior PnL total e fecha tudo quando a queda excede `MoneyTrailStop`.

## Detalhes de gestão de dinheiro

- **PnL total** é a soma do lucro realizado (`PnL`) e do PnL flutuante calculado a partir do preço de fechamento do candle, do passo de preço e do valor do passo.
- **Break-even** move o stop de proteção para `Entry + BreakEvenOffsetPips` (comprado) ou `Entry - BreakEvenOffsetPips` (vendido) quando o movimento excede `BreakEvenTriggerPips`.
- **Trailing stop** se desloca para mais perto do preço sempre que o ganho excede `TrailingStopPips`. Níveis trailing comprados só aumentam; níveis trailing vendidos só diminuem.
- **Trailing monetário** é ativado depois que o lucro de `MoneyTrailTrigger` é visto. A partir daí, o maior lucro é memorizado; perder mais de `MoneyTrailStop` a partir desse pico fecha todas as posições.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `TradeVolume` | Volume de cada bloco de entrada. |
| `MaxEntries` | Número máximo de blocos de volume a acumular. |
| `StopLossPips` | Distância do stop-loss em pips. |
| `TakeProfitPips` | Distância do take-profit em pips. |
| `TrailingStopPips` | Distância de trailing em pips. |
| `UseBreakEven` | Habilita o movimento do stop para break-even. |
| `BreakEvenTriggerPips` | Lucro necessário antes da ativação do break-even. |
| `BreakEvenOffsetPips` | Pips extras adicionados ao mover para break-even. |
| `UseBollingerExit` | Habilita saídas por toques na Bollinger band. |
| `BollingerPeriod` / `BollingerDeviation` | Configurações das Bollinger Bands. |
| `UseProfitMoneyTarget` / `ProfitMoneyTarget` | Chave e valor do alvo absoluto de lucro. |
| `UseProfitPercentTarget` / `ProfitPercentTarget` | Chave e valor do alvo percentual de lucro. |
| `EnableMoneyTrail` | Habilita o trailing stop monetário. |
| `MoneyTrailTrigger` | Lucro necessário antes de o trailing monetário se tornar ativo. |
| `MoneyTrailStop` | Drawdown permitido a partir do pico antes da saída. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuração MACD. |
| `CandleType` | Período de execução. |
| `SignalCandleType` | Período usado para o sinal MACD. |

## Notas de uso

- A estratégia depende de valores corretos de `PriceStep` e `StepPrice` do instrumento. Configure o ativo antes de iniciar.
- Se a conta não informar valor de portfólio (`Portfolio.CurrentValue` ou `Portfolio.BeginValue`), o take-profit percentual será ignorado automaticamente.
- Todos os comentários dentro do arquivo C# documentam a lógica de negociação em inglês para simplificar a manutenção futura.
