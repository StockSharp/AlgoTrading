# Estratégia de FitFul 13
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O consultor especialista FitFul 13 trabalha em torno de níveis de pivot semanais derivados da semana de trading anterior. Ele aguarda que a vela H1 atual (período padrão) reaja a uma das bandas de pivot e confirma o movimento com duas velas mais antigas de uma série de confirmação M15. Quando a confirmação está presente, a estratégia abre uma posição com níveis pré-calculados de stop-loss e take-profit derivados da mesma estrutura de pivots. Um trailing stop protege trades lucrativos quando o preço avança o suficiente.

## Lógica original
1. Calcular o preço típico e a estrutura de pivot da semana anterior: `PriceTypical`, `R1`, `S1`, níveis intermediários (`R0.5`, `S0.5`, `R1.5`, etc.) e as extensões de segundo/terceiro nível.
2. Observar a vela H1 mais recente. Se fechou em alta, buscar no corpo da vela precedente um cruzamento para cima de um dos níveis de pivot. Se tal cruzamento ocorrer, preparar parâmetros comprados: stop abaixo do suporte relevante, take-profit acima da resistência associada. Para fechamentos em baixa, a lógica espelhada prepara parâmetros vendidos.
3. Se o corpo da vela H1 não interagiu com nenhum pivot, verificar duas velas M15 anteriores. Duas mínimas consecutivas perfurando o mesmo nível confirmam configurações compradas, enquanto duas máximas caindo por um nível confirmam vendidas. Cada combinação é mapeada ao seu próprio par de stop/take.
4. Enviar uma ordem a mercado com o volume líquido configurado. O port StockSharp trabalha com posições líquidas, portanto a exposição oposta é zerada antes de abrir a nova operação. Os preços de stop-loss e take-profit são armazenados internamente e aplicados via saídas virtuais em novas velas.
5. Aplicar um trailing stop virtual: quando o lucro aberto ultrapassar `TrailingStopPips + TrailingStepPips`, mover o stop para `close - TrailingStopPips` (comprado) ou `close + TrailingStopPips` (vendido). O stop nunca recua e só é ajustado se o preço avançar pelo menos o passo de trailing.
6. Ignorar novos sinais se a posição líquida absoluta já equivale a `Volume × MaxPositions`.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
|------|------|--------|-----------|
| `CandleType` | `DataType` | H1 | Período principal usado para avaliar reações de pivot. |
| `ConfirmationCandleType` | `DataType` | M15 | Período inferior que fornece a confirmação de duas barras. |
| `Volume` | `decimal` | 0.1 | Volume de ordem líquida para cada entrada. |
| `MaxPositions` | `int` | 3 | Exposição líquida máxima expressa como múltiplos de `Volume`. |
| `IndentPips` | `decimal` | 3 | Deslocamento aplicado aos cálculos de stop-loss e take-profit baseados em pivots. |
| `TrailingStopPips` | `decimal` | 150 | Distância do trailing stop em pips. Definir como zero para desabilitar o trailing. |
| `TrailingStepPips` | `decimal` | 5 | Progresso de preço adicional mínimo (em pips) necessário antes de ajustar o trailing stop. |

## Notas sobre o port
- O StockSharp gerencia uma única posição líquida. A capacidade de hedge original é emulada zerando a exposição oposta quando uma nova entrada é realizada.
- A lógica de stop-loss, take-profit e trailing é implementada virtualmente. A estratégia fecha posições em atualizações de velas quando o preço cruza os níveis armazenados.
- Os pivots semanais são recalculados sempre que uma nova vela semanal é recebida. A confirmação padrão usa H1/M15, mas ambos os períodos podem ser ajustados através de parâmetros.
- Todos os comentários no código-fonte são escritos em inglês conforme exigido pelas diretrizes de conversão.
