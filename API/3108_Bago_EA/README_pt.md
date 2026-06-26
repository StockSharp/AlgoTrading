# Estratégia Bago EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o consultor especialista de MetaTrader "Bago EA". Opera com rompimentos de seguidor de tendência
confirmados por cruzamentos de média móvel e RSI, enquanto o túnel Vegas (par de EMA 144/169) fornece filtros espaciais e
âncoras de trailing.

## Lógica de Trading

1. **Preparação de indicadores**
   - Duas EMAs (períodos `FastPeriod` e `SlowPeriod`, método `MaMethod`, preço `MaAppliedPrice`).
   - EMAs do túnel Vegas (períodos 144 e 169, mesmo método/preço) para detectar o canal direcional.
   - RSI (`RsiPeriod`, `RsiAppliedPrice`) para confirmação.
   - Todas as conversões preço-para-pip usam o `PriceStep` do instrumento com ajuste de 3/5 dígitos como o EA original.
2. **Máquina de estados de cruzamento**
   - O cruzamento de EMA para cima/baixo e o cruzamento de RSI acima/abaixo de 50 são rastreados com temporizadores. Cada
     estado permanece ativo por `CrossEffectiveBars` candles e é redefinido pelo cruzamento oposto ou pelo tempo limite.
   - Os cruzamentos do túnel marcam quando o preço se move de um lado do túnel Vegas para o outro.
3. **Condições de entrada**
   - **Comprado**: tanto o cruzamento de EMA quanto o de RSI estão ativos para cima *e* o preço:
     - Fecha acima do túnel em pelo menos `TunnelBandWidthPips` mas não mais que `TunnelSafeZonePips`, com corpo de candle
       altista, ou
     - Fecha abaixo do túnel em `TunnelBandWidthPips`, sinalizando um rebote de baixo.
   - **Vendido**: lógica espelho com cruzamentos de EMA/RSI para baixo.
   - O trading é permitido apenas dentro das sessões habilitadas (Londres 07–16, Nova York 12–21, Tóquio 00–08, ou qualquer
     barra que feche após as 23:00).
4. **Gestão de ordens**
   - Novas posições são abertas com volume `TradeVolume`. Posições opostas são fechadas antes de reverter.
   - O stop inicial é definido em `StopLossPips` a partir do preço de fechamento. Os deslocamentos stop-para-túnel usam
     `StopLossToFiboPips`.
5. **Trailing e saídas parciais**
   - À medida que o preço avança além dos níveis do túnel Vegas, o stop se move:
     - Dentro do túnel, o stop para em `tunnel ± (TrailingStepX + StopLossToFibo)`.
     - Fora do túnel, um trailing fixo de `TrailingStopPips` é aplicado atrás do preço.
   - Saídas parciais fecham `PartialClose1Volume` em `TrailingStep1Pips` e `PartialClose2Volume` em `TrailingStep2Pips`
     uma vez que o preço viajou longe o suficiente da entrada.
   - Um cruzamento oposto de EMA/RSI fecha toda a posição imediatamente.
6. **Stops**
   - Ordens de proteção são mantidas como ordens de stop de mercado. São canceladas sempre que a posição é fechada.

## Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|--------|-----------|
| `TradeVolume` | decimal | 3 | Tamanho da ordem em lotes. |
| `StopLossPips` | decimal | 30 | Distância inicial de stop-loss. |
| `StopLossToFiboPips` | decimal | 20 | Buffer adicional ao estacionar stops em torno do túnel Vegas. |
| `TrailingStopPips` | decimal | 30 | Distância do trailing stop quando o preço sai do túnel. |
| `TrailingStep1Pips` | decimal | 55 | Primeira camada de lucro para saída parcial e realocação de stop. |
| `TrailingStep2Pips` | decimal | 89 | Segunda camada de lucro para saída parcial e trailing. |
| `TrailingStep3Pips` | decimal | 144 | Camada final antes de usar trailing puro. |
| `PartialClose1Volume` | decimal | 1 | Volume fechado em `TrailingStep1Pips`. |
| `PartialClose2Volume` | decimal | 1 | Volume fechado em `TrailingStep2Pips`. |
| `CrossEffectiveBars` | int | 2 | Número de barras durante as quais cruzamentos de EMA/RSI permanecem válidos. |
| `TunnelBandWidthPips` | decimal | 5 | Zona neutra em torno do túnel Vegas onde nenhuma operação é feita. |
| `TunnelSafeZonePips` | decimal | 120 | Distância máxima acima do túnel para entradas compradas (e abaixo para vendidas). |
| `EnableLondonSession` | bool | true | Permitir sinais durante as horas de Londres. |
| `EnableNewYorkSession` | bool | true | Permitir sinais durante as horas de Nova York. |
| `EnableTokyoSession` | bool | false | Permitir sinais durante as horas de Tóquio. |
| `FastPeriod` | int | 5 | Comprimento de EMA rápida. |
| `SlowPeriod` | int | 12 | Comprimento de EMA lenta. |
| `MaShift` | int | 0 | Deslocamento horizontal aplicado a todas as EMAs. |
| `MaMethod` | `MovingAverageType` | Exponential | Modo de suavização da média móvel. |
| `MaAppliedPrice` | `AppliedPriceType` | Close | Preço da candle enviado para as EMAs. |
| `RsiPeriod` | int | 21 | Comprimento de médio do RSI. |
| `RsiAppliedPrice` | `AppliedPriceType` | Close | Preço da candle enviado para o RSI. |
| `CandleType` | `DataType` | Período H1 | Série de candles para o cálculo. |

## Notas

- A estratégia mantém os estados dos indicadores mesmo fora do horário de trading, exatamente como no EA original.
- As ordens de stop são gerenciadas via API de alto nível (`SellStop`/`BuyStop`) para imitar as chamadas
  `PositionModify` do MetaTrader.
- Todos os comentários e estrutura seguem as diretrizes do repositório (tabulações para recuo, comentários em inglês).
