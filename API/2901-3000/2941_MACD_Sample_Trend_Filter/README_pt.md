# Estratégia MACD Sample com Filtro de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port direto do clássico consultor especialista **MACD Sample** do MetaTrader 5. Ela usa cruzamentos de MACD filtrados por um detector de tendência EMA. As ordens são dimensionadas com a propriedade padrão `Volume`, enquanto o gerenciamento de risco baseia-se em limites de pips configuráveis para o histograma MACD, take profit e trailing stop.

## Lógica principal

- **Indicadores**
  - `MovingAverageConvergenceDivergenceSignal` com períodos *(12, 26, 9)* fornece linhas MACD e de sinal.
  - `ExponentialMovingAverage` com período *26* atua como filtro de tendência.
- **Critérios de entrada**
  - **Comprado**: MACD está abaixo de zero, cruza acima da linha de sinal, tem magnitude acima do *Nível de Abertura MACD*, e a EMA está subindo.
  - **Vendido**: MACD está acima de zero, cruza abaixo da linha de sinal, tem magnitude acima do *Nível de Abertura MACD*, e a EMA está caindo.
- **Critérios de saída**
  - MACD cruza contra a posição com magnitude acima do *Nível de Fechamento MACD*.
  - Take profit atinge a distância em pips configurada a partir do preço de entrada.
  - Trailing stop (se ativado por lucro > distância de trailing) é atingido.
- **Mecânica do trailing stop**
  - Posições compradas ativam o trailing stop quando o preço máximo ultrapassa o preço de entrada pela distância de trailing. O stop é então mantido em *máximo − distância de trailing*.
  - Posições vendidas ativam o trailing stop quando o preço mínimo se move abaixo do preço de entrada pela distância de trailing. O stop é mantido em *mínimo + distância de trailing*.

## Parâmetros

| Parâmetro | Valor padrão | Descrição |
|-----------|--------------|-----------|
| `FastPeriod` | 12 | Período EMA rápida dentro do MACD. |
| `SlowPeriod` | 26 | Período EMA lenta dentro do MACD. |
| `SignalPeriod` | 9 | Período EMA de sinal dentro do MACD. |
| `TrendPeriod` | 26 | Comprimento do filtro de tendência EMA. |
| `MacdOpenLevelPips` | 3 | Magnitude mínima do MACD (em pips) necessária para abrir uma operação. |
| `MacdCloseLevelPips` | 2 | Magnitude mínima do MACD (em pips) necessária para fechar uma operação no cruzamento. |
| `TakeProfitPips` | 50 | Distância de take profit expressa em pips. |
| `TrailingStopPips` | 30 | Distância de trailing stop expressa em pips. Definir como 0 para desabilitar o trailing. |
| `CandleType` | Período de 15 minutos | Tipo de candle usado para cálculos. |

### Conversão de pips

O consultor especialista original usava a normalização de pips do MetaTrader (multiplicando por 10 para símbolos de 3/5 dígitos). A conversão segue a mesma ideia inspecionando `Security.PriceStep`:

- Se o passo de preço tem 3 ou 5 casas decimais, o tamanho do pip é `PriceStep * 10`.
- Caso contrário, o tamanho do pip equivale a `PriceStep`.
- Quando o passo de preço não está disponível, os limites baseados em pips recorrem a valores brutos.

## Notas de comportamento

- Posições são fechadas antes que novos sinais sejam avaliados, espelhando a implementação MT5.
- Instruções `LogInfo` relatam entradas, saídas e atualizações de trailing stop para depuração mais fácil.
- Ordens de proteção não são colocadas automaticamente; as saídas são gerenciadas dentro de `ProcessCandle` para imitar a lógica do EA.
- Use `Volume` para definir o tamanho base da operação. As reversões compensam automaticamente a exposição atual adicionando `Math.Abs(Position)` ao volume da ordem.

## Diferenças da versão MQL5

- O processamento ocorre em candles finalizados em vez de a cada tick. Isso evita sinais repetidos enquanto mantém comportamento determinístico.
- As verificações de trailing stop e take profit usam máximas e mínimas de candles para aproximar os hits de bid/ask do EA original.
- Quando `Security.PriceStep` está ausente, os parâmetros de pips atuam como distâncias absolutas de preço e devem ser ajustados manualmente.

Ajuste os limites de pips e o tipo de candle para se adequar ao instrumento negociado, especialmente ao portar para mercados com diferentes tamanhos de tick.
