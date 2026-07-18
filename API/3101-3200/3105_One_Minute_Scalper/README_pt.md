# Estratégia de Scalper de Um Minuto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista **1 MINUTE SCALPER** do MetaTrader 4 para a API de alto nível do StockSharp.
Mantém a confirmação de tendência multicamadas, o momentum multi-temporal e o filtro MACD de longo prazo do robô original
enquanto adapta os controles de risco ao modelo centrado em posições do StockSharp.

## Lógica Central

1. **Pilha de Tendência** – treze médias móveis lineares ponderadas (LWMA 3/5/8/10/12/15/30/35/40/45/50/55/200) devem estar
   alinhadas em ordem estrita. Operações compradas requerem que cada média mais curta esteja acima da próxima, enquanto as
   vendidas invertem a condição.
2. **Portão de Tendência Principal** – uma LWMA rápida adicional (padrão 6) deve permanecer acima da LWMA lenta (padrão 85)
   para comprados e abaixo para vendidos, espelhando a verificação rápido-vs-lento do EA.
3. **Estrutura de Candle** – entradas só são acionadas quando os padrões de sobreposição do script estão presentes: para
   comprados o mínimo de duas barras atrás deve estar abaixo do máximo anterior; para vendidos o mínimo anterior deve cair
   abaixo do máximo de duas barras atrás.
4. **Filtro de Momentum** – um indicador de momentum de 14 períodos calculado em um período superior (padrão candles de 15
   minutos) deve se desviar de 100 pelo menos nos limiares configurados em qualquer um dos últimos três valores. Isso reproduz
   as comparações `MomLevelB/MomLevelS`.
5. **Viés MACD Mensal** – um MACD construído no período MACD selecionado (padrão candles de 30 dias como proxy para dados
   mensais) deve mostrar a linha principal acima do sinal para comprados ou abaixo para vendidos.

## Gestão de Operações

- **Proteção Inicial** – as distâncias de stop-loss e take-profit são expressas em passos do instrumento (pontos). Quando uma
  posição abre, a estratégia converte essas contagens de passos para preços absolutos usando `Security.PriceStep`.
- **Movimento de Ponto de Equilíbrio** – após o preço mover `BreakEvenTriggerSteps` a favor, o stop é movido para a entrada
  mais `BreakEvenOffsetSteps` (para vendidos a lógica espelhada se aplica). O indicador é acionado uma vez por posição.
- **Trailing por Passos** – quando `TrailingStopSteps` é positivo, o stop segue o preço mais alto (ou mais baixo) desde a
  entrada pelo número especificado de passos.
- **Trailing Monetário** – uma vez que o lucro flutuante exceda `MoneyTrailTarget` (moeda), a estratégia rastreia o PnL
  flutuante máximo e fecha a posição se o recuo for igual a `MoneyTrailStop`.
- **Alvos Monetários/Percentuais** – alvos de take-profit absolutos ou opcionais fecham toda a exposição quando o PnL flutuante
  cruza os limiares configurados. O alvo percentual usa o valor inicial do portfólio capturado quando a estratégia começa.
- **Stop de Capital** – a estratégia monitora o capital máximo (valor do portfólio mais PnL aberto). Se o drawdown desse pico
  exceder `EquityRiskPercent`, todas as posições são fechadas, replicando a salvaguarda `AccountEquityHigh()` do EA.

## Parâmetros

| Parâmetro | Descrição |
| --- | --- |
| `Volume` | Volume de ordem para novas entradas. Adicionado à posição atual absoluta para que as reversões mudem a exposição imediatamente. |
| `FastMaPeriod` / `SlowMaPeriod` | Comprimentos de LWMA para o filtro de tendência principal. |
| `MomentumPeriod` | Comprimento do indicador de momentum no período superior. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Desvio absoluto mínimo de 100 necessário para confirmação de momentum comprado/vendido. |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | Configuração de MACD aplicada ao `MacdCandleType`. |
| `StopLossSteps` / `TakeProfitSteps` | Distâncias de stop protetor e alvo medidas em passos de preço. Definir como zero para desativar. |
| `TrailingStopSteps` | Distância do trailing stop baseado em passos (0 desativa). |
| `BreakEvenTriggerSteps` / `BreakEvenOffsetSteps` | Distância para acionar o movimento de ponto de equilíbrio e o offset aplicado ao mover o stop. |
| `UseMoneyTakeProfit`, `MoneyTakeProfit` | Habilitar e dimensionar o alvo de lucro flutuante baseado em moeda. |
| `UsePercentTakeProfit`, `PercentTakeProfit` | Habilitar e dimensionar o alvo de lucro flutuante como percentual do capital inicial. |
| `EnableMoneyTrailing`, `MoneyTrailTarget`, `MoneyTrailStop` | Configurar a lógica de trailing do lucro flutuante. |
| `UseEquityStop`, `EquityRiskPercent` | Habilitar o stop de drawdown e definir o percentual máximo de drawdown. |
| `CandleType` | Candles de trabalho principais (padrão 1 minuto). |
| `MomentumCandleType` | Candles de período superior para o indicador de momentum (padrão 15 minutos). |
| `MacdCandleType` | Candles usadas para o filtro de tendência MACD (padrão 30 dias ≈ mensal). |

## Diferenças vs. o Expert do MT4

- StockSharp usa posições líquidas, então a estratégia sempre mantém uma única posição agregada em vez de múltiplos tickets até
  `Max_Trades`. Reversões fecham a exposição existente antes de abrir na direção oposta.
- `PercentTakeProfit` referencia o valor do portfólio capturado no início em vez do `AccountBalance()` em constante mudança
  usado pelo MetaTrader, o que evita alvos ruidosos quando operações externas modificam o saldo.
- A lógica de saída baseada em dinheiro (`Take_Profit_In_Money` e `TRAIL_PROFIT_IN_MONEY2`) opera sobre o PnL flutuante em tempo
  real calculado a partir do preço de entrada médio da estratégia. Isso corresponde ao comportamento do EA mas dentro do
  framework de proteção do StockSharp.
- A plataforma deve fornecer feeds de candles para os períodos selecionados (`CandleType`, `MomentumCandleType`,
  `MacdCandleType`). Certifique-se de que os adaptadores que você usa suportam as resoluções solicitadas.

Ajuste os limiares para adequar ao seu instrumento e sessão. Pares com spreads estreitos ou altamente voláteis podem exigir
distâncias de passos maiores ou limiares de momentum maiores para reduzir o ruído.
