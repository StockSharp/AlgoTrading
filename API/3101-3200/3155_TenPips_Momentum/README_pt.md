# Estratégia de TenPips Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **estratégia TenPips** é um port do StockSharp do consultor especialista MetaTrader "10PIPS". Combina médias móvidas lineares ponderadas rápidas/lentas calculadas no período de negociação com uma confirmação de Momentum multi-período e um filtro MACD macro (mensal). A conversão espelha o módulo original de gestão de capital, incluindo proteção de ponto de equilíbrio, trailing baseado em pips e metas de lucro em capital/absolutos.

## Lógica de sinais

1. **Período primário** (parâmetro `CandleType`, padrão 15 minutos) fornece o fluxo de preços usado para as LWMAs rápidas e lentas calculadas no preço típico `(H + L + C) / 3`.
2. **Confirmação de Momentum em período superior** (`MomentumCandleType`, padrão 1 hora) converte a diferença de Momentum do StockSharp na proporção do MetaTrader. A distância absoluta de `100` nas últimas três barras concluídas deve exceder `MomentumThreshold` para que um trade seja armado.
3. **Filtro MACD macro** (`MacdCandleType`, padrão velas de 30 dias aproximando o período mensal do MetaTrader) requer que a linha principal do MACD esteja acima da linha de sinal para compras e abaixo para vendas.

Uma posição longa é aberta quando a vela anterior:
- fechou acima da LWMA rápida após mergulhar abaixo dela,
- a LWMA rápida está acima da LWMA lenta,
- qualquer uma das últimas três leituras de Momentum atende ao `MomentumThreshold`,
- o MACD macro é de alta.

Uma posição curta usa as condições simétricas (fechamento anterior abaixo da LWMA rápida, rápida abaixo de lenta, Momentum acima do limiar, MACD de baixa).

Como o StockSharp opera com um modelo de posição líquida, o port abre no máximo uma posição agregada por lado. Enviar uma compra enquanto vendido fecha automaticamente a parte vendida e deixa o volume longo solicitado.

## Gestão de risco e capital

- **Distâncias de proteção** – `StopLossPips` e `TakeProfitPips` traduzem pips do MetaTrader em offsets de preço usando o `PriceStep` do instrumento. Quando qualquer limite é atingido, a estratégia fecha toda a posição com uma ordem de mercado.
- **Trailing stop** – `TrailingStopPips` segue o preço mais alto (longo) ou mais baixo (curto) desde a entrada.
- **Ponto de equilíbrio** – quando habilitado, `BreakEvenTriggerPips` arma o stop e o desloca para a entrada mais o opcional `BreakEvenOffsetPips`.
- **Metas monetárias** – o trio `UseMoneyTakeProfit`, `UsePercentTakeProfit` e `EnableMoneyTrailing` replica o `TP_In_Money`, `TP_In_Percent` do EA e o bloqueio de trailing baseado em saldo. O PnL não realizado é medido a cada fechamento de vela.
- **Stop de capital** – `UseEquityStop` com `EquityRiskPercent` implementa a proteção original `UseEquityStop`/`TotalEquityRisk` fechando posições quando o drawdown do pico de capital excede o limiar.
- **Flag de saída MACD** – `UseMacdExit` espelha o interruptor `Exit` do EA, fechando posições antecipadamente quando o MACD macro vira contra o trade.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TradeVolume` | `0.01` | Volume de posição líquida usado para ordens de mercado (equivalente ao tamanho de lote do MetaTrader). |
| `CandleType` | Período `15m` | Período primário para as LWMAs rápidas/lentas e execução de trades. |
| `MomentumCandleType` | Período `1h` | Velas de período superior alimentando a confirmação de Momentum. |
| `MacdCandleType` | Período `30d` | Período macro (aproximação mensal) para confirmação MACD. |
| `FastMaPeriod` | `8` | Período da média móvel linear ponderada rápida. |
| `SlowMaPeriod` | `50` | Período da média móvel linear ponderada lenta. |
| `MomentumPeriod` | `14` | Lookback para a proporção de Momentum. |
| `MomentumThreshold` | `0.3` | Distância absoluta mínima de `100` (Momentum do MetaTrader) necessária nas últimas três barras do período superior. |
| `StopLossPips` | `20` | Stop-loss de proteção em pips do MetaTrader. Definir como zero para desabilitar. |
| `TakeProfitPips` | `50` | Take-profit de proteção em pips do MetaTrader. Definir como zero para desabilitar. |
| `TrailingStopPips` | `40` | Distância do trailing stop em pips (zero desabilita o trailing). |
| `UseBreakEven` | `true` | Habilita o comportamento de mover para ponto de equilíbrio. |
| `BreakEvenTriggerPips` | `30` | Lucro (pips) necessário antes da ativação do ponto de equilíbrio. |
| `BreakEvenOffsetPips` | `30` | Pips extras adicionados ao stop de ponto de equilíbrio após ativação. |
| `UseMoneyTakeProfit` | `false` | Fecha posições ao atingir a meta de lucro absoluto `MoneyTakeProfit`. |
| `MoneyTakeProfit` | `10` | Meta de lucro expressa em moeda de conta. |
| `UsePercentTakeProfit` | `false` | Fecha posições após ganhar `PercentTakeProfit` por cento do capital inicial. |
| `PercentTakeProfit` | `10` | Meta percentual baseada no capital inicial. |
| `EnableMoneyTrailing` | `true` | Habilitar trailing stop baseado em saldo usando `MoneyTrailTarget` / `MoneyTrailStop`. |
| `MoneyTrailTarget` | `40` | Lucro (moeda) necessário antes que o money trail seja armado. |
| `MoneyTrailStop` | `10` | Recuo permitido após armar o money trail. |
| `UseEquityStop` | `true` | Habilitar proteção de drawdown de capital. |
| `EquityRiskPercent` | `1` | Drawdown máximo do pico de capital antes de forçar posição neutra. |
| `UseMacdExit` | `false` | Fechar posições em sinal MACD oposto do período macro. |

## Notas de implementação

- A conversão de pips segue a lógica do EA: se o tick size do corretor for `0.00001` ou `0.001`, um pip equivale a dez ticks; caso contrário, o `PriceStep` bruto é usado.
- O indicador de Momentum do StockSharp produz uma diferença de preço. A estratégia converte isso para a proporção do MetaTrader `(Close / Close(period) * 100)` antes de aplicar `MomentumThreshold`.
- O port opera em um ambiente de netting e portanto não replica o martingale multi-ticket do EA (`IncreaseFactor`, `LotExponent`, `Max_Trades`). Em vez disso, ajusta o volume da ordem automaticamente ao alternar entre posições longas e curtas.
- Saídas de proteção e gestão de lucros enviam ordens de mercado, correspondendo ao comportamento original do advisor ao modificar tickets abertos.
- Os gráficos exibem os indicadores processados (LWMA rápida, LWMA lenta, Momentum, MACD) quando a visualização está disponível.

## Uso

1. Configure os períodos de velas para corresponder ao gráfico do MetaTrader e ao período superior usado pelo EA.
2. Ajuste os parâmetros de risco baseados em pips ao tamanho de ponto do instrumento. Zero desabilita o componente correspondente.
3. Habilite ou desabilite metas monetárias/percentuais, stop de capital e saída MACD de acordo com suas preferências de risco.
4. Inicie a estratégia; ela assinará os três períodos de tempo necessários, gerenciará posições de acordo com as regras originais e registrará quaisquer saídas de proteção acionadas pelas proteções baseadas em saldo ou capital.
