# 3103 — ADX EA (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
O "ADX EA" original do MetaTrader combina rompimentos do Average Directional Index com cruzamentos de +DI/−DI, confirmação de
momentum em período superior e um filtro MACD mensal. O port em C# replica esse fluxo de trabalho multi-filtro sobre a API de
alto nível do StockSharp. A estratégia se inscreve em três fluxos de candles:

1. **Período principal** (padrão 5 minutos) — aciona ADX, médias móveis lineares ponderadas, verificações de estrutura de preço
   e filtros de volume.
2. **Período de momentum** (padrão 15 minutos) — produz os desvios de momentum em torno da linha de base 100 que condicionam as
   entradas.
3. **Período de MACD** (padrão 30 dias) — espelha o MACD mensal que controla as saídas de posição.

## Lógica de Trading
- **Módulo de rompimento** – Quando habilitado, as operações compradas requerem:
  - ADX ou +DI acima de `EntryLevel` e a diferença entre +DI e −DI maior que `MinDirectionalDifference`.
  - A LWMA rápida acima da LWMA lenta, estrutura de candle altista (`Low[2] < High[1]`) e momentum crescente
    (`Momentum[1] > Momentum[2]`).
  - Pelo menos uma das últimas três leituras de momentum no período superior a se desviar de 100 em mais de
    `MomentumBuyThreshold`.
  - Volume crescente no período principal (`Volume[1] > Volume[2]` ou `Volume[1] > Volume[3]`).
  - MACD no período mensal altista (`MacdMain[1] > MacdSignal[1]`).
  - ADX acima de `ExitLevel` para confirmar a fortaleza geral do trend.

  Os rompimentos vendidos aplicam a lógica simétrica com dominância de −DI, estrutura baixista (`Low[1] < High[2]`), momentum
  abaixo de 100 em `MomentumSellThreshold` e uma comparação MACD baixista.

- **Módulo de cruzamento** – Quando ativo, procura +DI cruzando acima de −DI (comprados) ou −DI cruzando acima de +DI
  (vendidos). Filtros opcionais refletem o EA original:
  - `RequireAdxSlope` exige que ADX seja maior que a leitura anterior.
  - `ConfirmCrossOnBreakout` adiciona as mesmas verificações de limiar de rompimento na barra de cruzamento.
  - `MinAdxMainLine` impõe uma fortaleza mínima de ADX durante o cruzamento.
  - O alinhamento de LWMA, inclinação de momentum, expansão de volume e polaridade de MACD ainda devem concordar com a
    direção pretendida.

- **Piramidação** – Cada nova ordem adiciona volume de acordo com `LotExponent`. A estratégia trata `TradeVolume` como o
  tamanho de lote base e o incrementa por `LotExponent^n`, onde `n` é o número de etapas já abertas. `MaxTrades` limita a
  quantidade de volume líquido que pode ser acumulado.

## Gestão de Risco
- **Ordens de proteção** – `TakeProfitSteps` e `StopLossSteps` são passados ao `StartProtection` e expressos em passos de
  preço do instrumento.
- **Trailing stop** – `TrailingStopSteps` mantém uma barreira de trailing manual além do melhor preço de fechamento.
- **Ponto de equilíbrio** – Quando `UseBreakEven` está habilitado, o stop é ajustado após o preço avançar `BreakEvenTrigger`
  passos e pode deslocar o stop `BreakEvenOffset` passos.
- **Saída por MACD** – Quando `EnableMacdExit` é verdadeiro, a relação MACD mensal fecha os comprados quando MACD cai abaixo
  de seu sinal (e vice-versa para vendidos), correspondendo às rotinas `Close_BUY`/`Close_SELL` do EA.
- **Stop de capital** – `UseEquityStop` rastreia a curva de lucro flutuante e liquida posições assim que o drawdown atinge
  `TotalEquityRisk` por cento.

As funcionalidades que dependiam de alvos em moeda da conta ("Take Profit in Money", "Trailing Profit in Money", etc.) não estão
portadas porque as estratégias do StockSharp tipicamente gerenciam a lógica de proteção através de distâncias de stop e o
serviço de proteção integrado. Todos os outros pontos de decisão do EA são preservados com equivalentes de indicadores.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `TradeVolume` | 0.01 | Tamanho de lote base para a primeira entrada. |
| `CandleType` | Período de 5m | Série de candles principal para lógica ADX/LWMA. |
| `MomentumCandleType` | Período de 15m | Período superior para o filtro de desvio de momentum. |
| `MacdCandleType` | Período de 30 dias | Período que alimenta o filtro de saída MACD. |
| `FastMaPeriod` | 6 | Comprimento da média móvel linear ponderada rápida. |
| `SlowMaPeriod` | 85 | Comprimento da média móvel linear ponderada lenta. |
| `AdxPeriod` | 14 | Período do Average Directional Index. |
| `MomentumPeriod` | 14 | Período do indicador de momentum no período superior. |
| `MacdFastPeriod` | 12 | Período de EMA rápido dentro do filtro de saída MACD. |
| `MacdSlowPeriod` | 26 | Período de EMA lento dentro do filtro de saída MACD. |
| `MacdSignalPeriod` | 9 | Período de SMA de sinal dentro do filtro de saída MACD. |
| `EnableBreakoutStrategy` | true | Alternância para o ramo de rompimento ADX. |
| `EnableCrossStrategy` | true | Alternância para o ramo de cruzamento DI. |
| `UseTrendFilter` | true | Impõe dominância de +DI para comprados e −DI para vendidos durante rompimentos. |
| `RequireAdxSlope` | true | Requer que ADX suba ao avaliar cruzamentos DI. |
| `ConfirmCrossOnBreakout` | true | Adiciona limiares de rompimento ao módulo de cruzamento. |
| `EnableMacdExit` | true | Habilita a rotina de saída baseada em MACD. |
| `EntryLevel` | 10 | Nível mínimo de ADX/+DI/−DI usado pelos rompimentos. |
| `ExitLevel` | 10 | Fortaleza mínima de ADX que permite novas entradas. |
| `MinDirectionalDifference` | 10 | Diferença requerida entre +DI e −DI. |
| `MinAdxMainLine` | 10 | Nível mínimo de ADX durante cruzamentos DI. |
| `MomentumBuyThreshold` | 0.3 | Desvio requerido de 100 para confirmação de momentum altista. |
| `MomentumSellThreshold` | 0.3 | Desvio requerido de 100 para confirmação de momentum baixista. |
| `MaxTrades` | 10 | Número máximo de etapas de piramidação. |
| `LotExponent` | 1.44 | Multiplicador de volume para cada etapa adicional. |
| `TakeProfitSteps` | 50 | Distância, em passos de preço, para a ordem de take-profit. |
| `StopLossSteps` | 20 | Distância, em passos de preço, para a ordem de stop-loss. |
| `TrailingStopSteps` | 40 | Distância do trailing stop manual em passos de preço. |
| `UseBreakEven` | true | Ativa a lógica de realocação do ponto de equilíbrio. |
| `BreakEvenTrigger` | 30 | Passos de movimento favorável necessários antes de armar o ponto de equilíbrio. |
| `BreakEvenOffset` | 30 | Passos adicionais ao preço de entrada ao mover o stop. |
| `UseEquityStop` | true | Habilita a saída de emergência baseada em drawdown. |
| `TotalEquityRisk` | 1 | Percentual de drawdown permitido antes de fechar todas as posições. |

## Dicas de Uso
- Alinhe `MomentumCandleType` e `MacdCandleType` com seu período principal para imitar o mapeamento de período original (p. ex.,
  gráfico de 5 minutos → momentum de 15 minutos → MACD mensal).
- Ajuste `EntryLevel`, `MinDirectionalDifference` e `MinAdxMainLine` juntos; diminuir os três afrouxa consideravelmente o filtro
  de rompimento.
- `LotExponent` maior que 1.0 recria o escalonamento estilo martingale do EA. Configure para 1.0 para manter os tamanhos de
  posição constantes.
