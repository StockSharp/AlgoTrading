# Estratégia Hercules A.T.C. 2006
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Hercules A.T.C. 2006 é uma estratégia de seguidor de tendência em períodos de tempo mais altos que recria o consultor especialista
do MetaTrader publicado em 2006. A versão StockSharp escuta velas concluídas no período de tempo primário, detecta cruzamentos
de alta/baixa entre uma EMA(1) rápida e uma SMA(72) lenta, e abre operações apenas quando filtros adicionais confirmam o
rompimento. A estratégia divide sua posição em duas tranches com níveis de take-profit independentes e ajusta o stop à medida
que o preço avança.

## Indicadores e dados

- **Velas primárias:** configuráveis (padrão: velas de 1 hora).
- **MA rápida:** EMA com comprimento `FastMaPeriod` (padrão: 1).
- **MA lenta:** SMA com comprimento `SlowMaPeriod` (padrão: 72).
- **Filtro RSI:** RSI de comprimento `RsiLength` no `RsiTimeFrame` (padrão: 1 hora).
- **Envelope diário:** SMA de comprimento `DailyEnvelopePeriod` no `DailyEnvelopeTimeFrame`
  com deslocamento de ±`DailyEnvelopeDeviation` por cento.
- **Envelope H4:** SMA de comprimento `H4EnvelopePeriod` no `H4EnvelopeTimeFrame`
  com deslocamento de ±`H4EnvelopeDeviation` por cento.
- **Máximo/mínimo rolante:** máximo mais alto e mínimo mais baixo das últimas `HighLowHours`
  horas no período de tempo primário.

## Parâmetros

| Nome | Padrão | Descrição |
| --- | --- | --- |
| `TriggerPips` | 38 | Deslocamento em pips adicionado/subtraído ao preço de cruzamento antes de disparar uma ordem. |
| `TrailingStopPips` | 90 | Distância do stop móvel em pips (0 desativa o trailing). |
| `TakeProfit1Pips` | 210 | Primeira distância de take-profit em pips para reduzir metade da posição. |
| `TakeProfit2Pips` | 280 | Distância final de take-profit em pips para fechar a posição restante. |
| `FastMaPeriod` | 1 | Comprimento da EMA rápida usada no detector de cruzamento. |
| `SlowMaPeriod` | 72 | Comprimento da SMA lenta de referência. |
| `StopLossLookback` | 4 | Número de velas concluídas usadas para calcular o preço inicial do stop. |
| `HighLowHours` | 10 | Tamanho da janela rolante (em horas) usada para o filtro de rompimento. |
| `BlackoutHours` | 144 | Período de resfriamento (em horas) após fechar uma operação antes de permitir uma nova entrada. |
| `RsiLength` | 10 | Comprimento do RSI no filtro de período de tempo superior. |
| `RsiUpper` | 55 | Valor mínimo do RSI necessário para permitir entradas compradas. |
| `RsiLower` | 45 | Valor máximo do RSI permitido antes de bloquear entradas vendidas. |
| `DailyEnvelopePeriod` | 24 | Comprimento da SMA para o filtro de envelope diário. |
| `DailyEnvelopeDeviation` | 0.99 | Desvio do envelope diário em porcentagem. |
| `H4EnvelopePeriod` | 96 | Comprimento da SMA para o filtro de envelope de quatro horas. |
| `H4EnvelopeDeviation` | 0.1 | Desvio do envelope de quatro horas em porcentagem. |
| `CandleType` | 1 hora | Tipo de vela de trabalho primário. |
| `RsiTimeFrame` | 1 hora | Tipo de vela usado para o filtro RSI. |
| `DailyEnvelopeTimeFrame` | 1 dia | Tipo de vela usado para o envelope diário. |
| `H4EnvelopeTimeFrame` | 4 horas | Tipo de vela usado para o envelope de quatro horas. |

## Regras de trading

1. **Detecção de cruzamento**
   - Observar os valores de EMA(1) e SMA(72) das últimas três barras concluídas.
   - Detectar um sinal de alta quando a EMA cruza acima da SMA em qualquer uma das duas barras anteriores.
   - Detectar um sinal de baixa quando a EMA cruza abaixo da SMA em qualquer uma das duas barras anteriores.
   - Armazenar o preço de cruzamento (média dos valores rápido e lento) e iniciar uma janela de ativação de duas barras.

2. **Condição de ativação**
   - Calcular `TriggerPrice = CrossPrice ± TriggerPips` (convertido para unidades de preço).
   - A ativação permanece válida por duas velas primárias após o momento do cruzamento.
   - Comprados exigem que a máxima da vela alcance ou supere o preço de ativação de alta.
   - Vendidos exigem que a mínima da vela alcance ou rompa o preço de ativação de baixa.

3. **Filtros de entrada**
   - Sem posição existente e sem resfriamento ativo (`BlackoutHours`).
   - Filtro RSI: `RSI > RsiUpper` para comprados, `RSI < RsiLower` para vendidos.
   - Filtro de rompimento: o fechamento atual deve exceder a máxima rolante para comprados ou cair abaixo da mínima rolante para vendidos.
   - Confirmação de envelope: o fechamento atual deve estar acima de ambas as bandas superiores para comprados ou abaixo de ambas as bandas inferiores para vendidos.

4. **Execução de ordens**
   - Enviar uma ordem de mercado usando o volume da estratégia (padrão: 2 unidades, ou seja, duas sub-posições iguais).
   - Stop-loss: mínima (comprado) ou máxima (vendido) da `StopLossLookback`-ésima vela anterior.
   - Níveis de take-profit: `TakeProfit1Pips` para a primeira metade, `TakeProfit2Pips` para o restante.
   - Iniciar um temporizador de bloqueio para impedir novas entradas por `BlackoutHours` horas.

5. **Gerenciamento de posição**
   - O stop móvel é ativado imediatamente se `TrailingStopPips` > 0 e se move apenas a favor da operação.
   - Reduzir metade da posição no primeiro nível de take-profit.
   - Fechar a posição restante quando o take-profit final for acionado, o stop-loss for atingido ou o preço cruzar o stop móvel.

## Gerenciamento de risco

- Os stops são sempre derivados de velas concluídas para reduzir o ruído intrabarra.
- Dois alvos de take-profit garantem lucros parciais antes de deixar a operação continuar.
- Stops móveis garantem que os ganhos sejam protegidos após o mercado se mover na direção desejada.
- Um longo período de bloqueio (padrão: 144 horas) evita reentrada rápida após um rompimento e espelha o comportamento original do EA.

## Notas

- O port do StockSharp preserva a ideia original de gerenciamento de capital ao definir o volume padrão da estratégia em dois unidades, de modo que a saída parcial mantém metade da posição em execução.
- Os valores de deslocamento do envelope do MetaTrader são aproximados usando os valores mais recentes do envelope, pois o deslocamento para frente não é suportado pela API de alto nível.
- A estratégia requer informações sobre o passo de preço para traduzir corretamente as distâncias em pips; certifique-se de que os metadados do instrumento estejam preenchidos.
