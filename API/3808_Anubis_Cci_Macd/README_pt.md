# Estratégia de Anúbis CCI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Converte o MetaTrader 4 consultor especialista "Anubis" para o StockSharp API de alto nível.
- Usa um filtro de índice de canais de commodities (CCI) de 4 horas junto com um cruzamento MACD de 15 minutos.
- Aplica dimensionamento de posição adaptável, stop-loss, proteção de equilíbrio, saídas orientadas por ATR e um take-profit baseado no desvio padrão.

## Lógica estratégica
1. **Dados**
   - Período principal: velas de 15 minutos (`SignalCandleType`), usadas para cálculos de MACD e ATR.
   - Prazo maior: velas de 4 horas (`TrendCandleType`), usadas para filtragem CCI e medição de desvio padrão.
2. **Indicadores**
   - `CommodityChannelIndex` com período configurável na série 4H.
   - `StandardDeviation` (comprimento 30) em 4H fecha para estimar a distância de realização do lucro.
   - `MovingAverageConvergenceDivergenceSignal` (rápido/lento/sinal configurável) em velas de 15 milhões.
   - `AverageTrueRange` (comprimento 12) em velas de 15 milhões para saídas baseadas em volatilidade.
3. **Inscrições**
   - **Venda**: quando 4H CCI está acima de `CciThreshold`, os dois valores anteriores de MACD mostram um cruzamento de baixa (MACD cruzando abaixo de seu sinal), MACD foi positivo, não há posições compradas abertas e o preço se moveu pelo menos `PriceFilterPoints` desde a última entrada vendida.
   - **Longo**: condição simétrica com CCI abaixo de `-CciThreshold`, MACD cruzando para cima enquanto negativo, sem posições curtas abertas e o filtro de distância mínima satisfeito.
4. **Gerenciamento de riscos**
   - O volume base é definido por `VolumeValue` e é dimensionado pelo patrimônio da conta (2× acima de 14k, 3,2× acima de 22k) e por `LossFactor` após uma negociação perdedora.
   - O máximo de negociações simultâneas por direção é limitado por `MaxLongTrades` e `MaxShortTrades`.
   - Stop-loss rígido colocado virtualmente em `StopLossPoints * PriceStep` do preço médio de entrada.
   - O ponto de equilíbrio é ativado quando o preço avança `BreakevenPoints` e fecha imediatamente a posição se o preço retornar à entrada.
5. **Saídas**
   - O take-profit de desvio padrão fecha a posição quando o preço se move `StdDevMultiplier * StdDev` a favor.
   - As saídas agressivas são acionadas quando o intervalo da vela anterior excede `CloseAtrMultiplier * ATR`.
   - As saídas de desaceleração MACD exigem lucro suficiente (`ProfitThresholdPoints`) e uma reversão na inclinação MACD (MACD anterior menor ou maior que duas barras atrás, dependendo da direção).
   - O stop protetor fecha a negociação se o preço ultrapassar a distância do stop loss ou voltar à entrada após a ativação do ponto de equilíbrio.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `VolumeValue` | Volume básico do pedido. |
| `CciThreshold` | Limite absoluto para o filtro 4H CCI. |
| `CciPeriod` | Período do indicador 4H CCI. |
| `StopLossPoints` | Distância de stop-loss em pontos. |
| `BreakevenPoints` | Lucre em pontos necessários para armar o ponto de equilíbrio. |
| `MacdFastPeriod` | Período rápido de EMA para MACD. |
| `MacdSlowPeriod` | Período EMA lenta para MACD. |
| `MacdSignalPeriod` | Período de sinal EMA para MACD. |
| `LossFactor` | Multiplicador de volume aplicado após uma negociação perdida. |
| `MaxShortTrades` | Número máximo de entradas curtas simultâneas. |
| `MaxLongTrades` | Número máximo de entradas longas simultâneas. |
| `CloseAtrMultiplier` | Multiplicador de ATR para saídas antecipadas. |
| `ProfitThresholdPoints` | Buffer de lucro adicional (pontos) antes da saída de MACD. |
| `StdDevMultiplier` | Multiplicador de desvio padrão para o take-profit. |
| `PriceFilterPoints` | Movimento do preço mínimo entre entradas consecutivas. |
| `SignalCandleType` | Período principal para MACD e ATR. |
| `TrendCandleType` | Prazo maior para CCI e desvio padrão. |

## Notas
- A estratégia depende de metadados `Security.PriceStep` válidos para traduzir parâmetros baseados em pontos em distâncias de preços.
- A lógica de proteção é implementada por meio de verificações explícitas em vez de ordens pendentes de stop/limit, espelhando o comportamento original EA com paradas virtuais.
- A versão Python é omitida intencionalmente de acordo com as instruções da tarefa.
