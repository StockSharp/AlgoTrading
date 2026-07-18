# Estratégia Crypto Analysis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port para StockSharp do expert advisor "Crypto Analysis" do MetaTrader 4. Ela procura rompimentos que ocorrem depois que o preço toca a banda externa de Bollinger no timeframe principal de negociação, enquanto a estrutura de mercado permanece baixista (LWMA rápida abaixo da LWMA lenta). O sistema só permite operações quando um surto de momentum em timeframe superior e um filtro MACD mensal concordam com a direção desejada. Depois de entrar no mercado, a posição é gerenciada por um bloco de proteção em camadas que espelha o EA original: stops baseados em pips, trailing baseado em dinheiro, realocação para break-even e controles de drawdown da carteira.

## Lógica de negociação
- **Timeframe de sinal:** configurável (M15 por padrão). Todas as regras de entrada/saída são avaliadas nesses candles.
- **Gatilho de volatilidade:** a mínima do candle anterior deve tocar ou perfurar a banda inferior de Bollinger (20, 2) para preparar uma configuração comprada; um toque na banda superior prepara uma configuração vendida.
- **Filtro de tendência:** ambos os cenários exigem que a média móvel linearmente ponderada rápida (LWMA, padrão 6) permaneça abaixo da LWMA lenta (padrão 85), replicando a checagem de viés baixista do EA.
- **Confirmação RSI:** RSI(14) precisa estar acima de 50 para compras e abaixo de 50 para vendas.
- **Surto de momentum:** o desvio absoluto máximo dos três últimos valores Momentum(14) do timeframe superior em relação à linha-base 100 deve exceder os limiares de compra/venda. Isso captura os picos de momentum usados pelo código MQL.
- **Filtro MACD mensal:** um MACD mensal separado (candles de 30 dias por padrão) (12, 26, 9) confirma a direção; compras exigem MACD principal acima do sinal, vendas exigem o oposto.
- **Execução de entrada:** quando todos os filtros se alinham, a estratégia abre uma ordem a mercado. Posições opostas são zeradas antes da reversão para manter uma única posição líquida, espelhando o comportamento do EA de fechar operações contrárias.

## Gestão de posição
- **Stop e alvo iniciais:** distâncias configuráveis em pips são convertidas a partir do tamanho de tick do instrumento usando o mesmo tratamento de 5 dígitos/3 dígitos do EA (passos `0.00001` e `0.001` são multiplicados por 10).
- **Trailing stop:** após a formação de uma nova máxima (compra) ou mínima (venda), o stop é puxado atrás do preço por `TrailingStopPips`, sempre respeitando o melhor nível alcançado.
- **Break-even:** quando o lucro atinge `BreakEvenTriggerPips`, o stop é movido para o preço de entrada mais `BreakEvenOffsetPips` (compra) ou menos o offset (venda).
- **Metas monetárias:** limites opcionais de lucro absoluto ou percentual fecham a posição assim que o PnL flutuante atinge o nível solicitado.
- **Trailing monetário:** quando o lucro não realizado excede `MoneyTrailTarget`, a estratégia acompanha o pico e fecha a posição se a devolução for igual ou superior a `MoneyTrailStop`.
- **Stop de patrimônio:** o patrimônio flutuante (valor atual da carteira mais PnL não realizado) é monitorado; se o drawdown a partir do pico ultrapassar `EquityRiskPercent`, a posição é zerada.

## Dados multi-timeframe
Três assinaturas são registradas automaticamente:
1. Série principal de candles para as regras de Bollinger/LWMA/RSI.
2. Candles de timeframe superior para o filtro de momentum (H1 por padrão).
3. Candles mensais para a confirmação MACD (barras de 30 dias por padrão).

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `OrderVolume` | Tamanho base da ordem. Posições opostas são fechadas antes de abrir uma nova. |
| `UseMoneyTakeProfit` | Habilita a meta absoluta de take-profit monetário. |
| `MoneyTakeProfit` | Lucro na moeda da carteira que dispara uma saída quando `UseMoneyTakeProfit` é verdadeiro. |
| `UsePercentTakeProfit` | Habilita a meta de take-profit percentual calculada a partir do patrimônio inicial. |
| `PercentTakeProfit` | Percentual de lucro necessário para fechar a posição quando a meta percentual está ativa. |
| `EnableMoneyTrailing` | Ativa o bloco de trailing baseado em dinheiro. |
| `MoneyTrailTarget` | Nível de lucro que inicia o trailing monetário. |
| `MoneyTrailStop` | Devolução máxima permitida do lucro depois que `MoneyTrailTarget` foi alcançado. |
| `StopLossPips` | Distância inicial do stop-loss em pips. |
| `TakeProfitPips` | Distância inicial do take-profit em pips. |
| `TrailingStopPips` | Distância do trailing stop em pips. |
| `UseBreakEven` | Habilita a realocação do stop para break-even. |
| `BreakEvenTriggerPips` | Lucro em pips necessário antes de ativar a proteção break-even. |
| `BreakEvenOffsetPips` | Pips adicionais somados ao preço de entrada ao posicionar o stop de break-even. |
| `FastMaPeriod` | Comprimento da LWMA rápida calculada sobre preço típico. |
| `SlowMaPeriod` | Comprimento da LWMA lenta calculada sobre preço típico. |
| `MomentumPeriod` | Período do indicador Momentum no timeframe superior. |
| `MomentumBuyThreshold` | Desvio mínimo de momentum para entradas compradas. |
| `MomentumSellThreshold` | Desvio mínimo de momentum para entradas vendidas. |
| `MacdFastLength` | Comprimento da EMA rápida para o filtro MACD de timeframe superior. |
| `MacdSlowLength` | Comprimento da EMA lenta para o filtro MACD de timeframe superior. |
| `MacdSignalLength` | Comprimento do sinal para o filtro MACD de timeframe superior. |
| `UseEquityStop` | Habilita a proteção contra drawdown da carteira. |
| `EquityRiskPercent` | Percentual permitido de drawdown do patrimônio antes de fechar a posição forçadamente. |
| `CandleType` | Timeframe primário usado para entradas. |
| `MomentumCandleType` | Timeframe superior usado para confirmação de momentum. |
| `MacdCandleType` | Timeframe superior usado para confirmação MACD. |

## Notas
- O port StockSharp mantém uma única posição líquida, correspondendo ao EA que fecha ordens opostas antes de abrir uma nova operação.
- Todas as regras de proteção operam em candles fechados para replicar o processamento de "nova barra" do script original.
- Ao usar símbolos sintéticos ou instrumentos sem tamanho de pip padrão, ajuste `StopLossPips` e parâmetros relacionados ao valor de tick da bolsa.
