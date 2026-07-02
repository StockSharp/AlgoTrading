# Estratégia inteligente de seguidor de tendências
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Smart Trend Follower** é uma versão StockSharp do MetaTrader 5 consultor especialista *Smart Trend Follower*. O
O sistema original alterna entre um cruzamento de média móvel contrário e uma configuração de acompanhamento de tendência que usa estocástica
confirmação. Ele escala para posições com um multiplicador de volume semelhante ao martingale e mantém um take-profit/stop-loss compartilhado
para cada cesta direcional. A versão StockSharp mantém o mesmo comportamento ao usar o API de alto nível (vela
subscrições, vinculações de indicadores e ordens de mercado).

## Lógica de Sinais
Dois mecanismos de sinal independentes estão disponíveis e podem ser alternados com o parâmetro `SignalMode`:

1. **CrossMa** – replica o crossover contrário original. Quando o rápido SMA cruza *abaixo* do lento SMA (rápido < lento
mas anteriormente rápido > lento) a estratégia abre ou calcula a média das posições longas. Quando o rápido SMA cruza *acima* do lento
SMA (rápido > lento, mas anteriormente rápido < lento) abre ou calcula a média dos shorts.
2. **Tendência** – segue o modo de tendência original que requer confirmação do oscilador estocástico. Um sinal de alta
aparece quando o rápido SMA permanece acima do lento SMA, a vela fecha mais alto do que abriu e o valor %K estocástico
está igual ou inferior a 30. Um sinal de baixa requer rápido < lento, um corpo de vela de baixa e %K estocástico igual ou superior a 70.

Os sinais são avaliados apenas em velas finalizadas. Sempre que um novo sinal chega enquanto posições opostas ainda estão abertas, o
estratégia primeiro liquida a cesta adversária e só depois processa novas entradas para ficar alinhada com a direção do
sinal atual.

## Escala de posição
A estratégia reproduz a lógica MQL martingale:

- O primeiro pedido usa `InitialVolume` lotes.
- Cada ordem de média adicional multiplica o volume anterior por `Multiplier` (valores ≤ 1 desativam o crescimento do volume).
- Uma nova ordem média para a direção ativa é permitida somente depois que o mercado se move em `LayerDistancePips` pips de distância
do melhor preço de entrada da cesta atual (menor long fill ou maior short fill).
- Os volumes são normalizados usando os limites do instrumento `VolumeStep`, `VolumeMin` e `VolumeMax` quando disponíveis.

## Gestão de risco
Para cada cesta direcional, a estratégia rastreia um preço de equilíbrio compartilhado (média ponderada pelo volume de todos os preenchimentos):

- `TakeProfitPips` define a distância entre o preço médio de entrada e o lucro da cesta. Cestos longos saem quando o
a vela alta toca esse nível, cestos curtos quando a vela baixa atinge esse nível. Defina como 0 para desativar as metas de lucro.
- `StopLossPips` reflete o comportamento das saídas de proteção. Cestos longos fecham quando a mínima da vela quebra abaixo do stop,
cestos curtos quando a vela passa acima deles. Defina como 0 para desabilitar a parada de proteção.

As ordens de saída são executadas através de ordens de mercado quando a próxima vela concluída confirma que o nível foi atingido. O
estratégia mantém sinalizadores `_longExitRequested` e `_shortExitRequested` para evitar envios de saída duplicados enquanto os preenchimentos são
ainda pendente.

## Parâmetros
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `SignalMode` | enumeração (`CrossMa`, `Trend`) | `CrossMa` | Seleciona o mecanismo de sinal (cruzamento contrário ou tendência com filtro estocástico). |
| `CandleType` | `DataType` | Período de 30 minutos | Série de velas primárias usadas para cálculos e geração de sinal. |
| `InitialVolume` | decimal | `0.01` | Tamanho base do pedido em lotes para a primeira entrada de qualquer cesta. |
| `Multiplier` | decimal | `2` | Multiplicador de volume aplicado a cada ordem de média adicional. |
| `LayerDistancePips` | decimal | `200` | Distância mínima do pip da melhor entrada antes de adicionar outra ordem na mesma direção. |
| `FastPeriod` | interno | `14` | Período da média móvel simples rápida. |
| `SlowPeriod` | interno | `28` | Período da média móvel simples lenta (deve ser maior que `FastPeriod`). |
| `StochasticKPeriod` | interno | `10` | Comprimento de lookback para a linha %K do oscilador estocástico. |
| `StochasticDPeriod` | interno | `3` | Comprimento de suavização para a linha %D estocástica. |
| `StochasticSlowing` | interno | `3` | Suavização adicional aplicada a %K antes do cálculo de %D. |
| `TakeProfitPips` | decimal | `500` | Distância em pips da entrada média onde o lucro da cesta é colocado. Defina 0 para desativar. |
| `StopLossPips` | decimal | `0` | Distância de parada protetora em pips. Defina 0 para desativar a parada brusca. |

## Notas de implementação
- O tamanho do pip é derivado do instrumento `PriceStep` e `Decimals`, correspondendo à noção de “ponto” MetaTrader (por exemplo
0,0001 para cotações de câmbio de 5 dígitos).
- O rastreamento de posição usa duas listas de objetos `PositionEntry` para espelhar a contabilidade por ticket de MetaTrader. As entradas são
estilo FIFO reduzido quando negociações opostas fecham parte de uma cesta.
- Todos os cálculos do indicador dependem da ligação de alto nível API de StockSharp (`SubscribeCandles().BindEx(...)`). Sem chamadas manuais
para `GetValue` são obrigatórios e os indicadores nunca são injetados em `Strategy.Indicators`.
- A estratégia chama `StartProtection()` no início, permitindo que StockSharp gerencie módulos globais de controle de risco (ponto de equilíbrio,
verificações de margem, etc.).
- Como StockSharp consolida posições líquidas por direção, as posições opostas são totalmente fechadas antes que novas entradas sejam
avaliado. Isso mantém a implementação determinística e estreitamente alinhada com o comportamento original EA.

## Arquivos
- `CS/SmartTrendFollowerStrategy.cs` – Implementação C# da estratégia usando o StockSharp API de alto nível.
