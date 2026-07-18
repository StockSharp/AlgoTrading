# Estratégia MA Crossover ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **MA Crossover ADX** é um port direto do expert advisor `MA_Crossover_ADX` do MetaTrader. Ela combina a inclinação de uma média móvel exponencial (EMA) com confirmação do Average Directional Index (ADX) para participar apenas de ambientes com tendência. A implementação StockSharp processa candles concluídos de um timeframe configurável e sincroniza as atualizações de EMA e ADX antes de emitir sinais. Distâncias protetoras de stop loss e take profit são anexadas automaticamente a cada nova posição usando os parâmetros de risco baseados em pontos da estratégia.

## Indicadores e dados
- **Média móvel exponencial (EMA):** atua como filtro primário de tendência. A estratégia acompanha os três últimos valores de EMA para calcular duas inclinações consecutivas, imitando as verificações `StateEMA(0)` e `StateEMA(1)` do EA original.
- **Average Directional Index (ADX):** fornece a linha principal de força da tendência e os indicadores direcionais positivo/negativo (DI+/DI-). O spread entre DI+ e DI- replica a condição `StateADX(0)` do EA, enquanto a linha principal impõe um limiar mínimo de força.
- **Série de preços de fechamento:** o fechamento do candle anterior é comparado à EMA anterior para garantir que o mercado se afastou da média móvel antes da entrada.

Todos os indicadores operam na mesma assinatura de candles, garantindo que os valores de EMA e ADX estejam finalizados para a mesma barra exata antes de qualquer decisão.

## Lógica de negociação
### Entrada comprada
1. A inclinação atual da EMA (`EMA[0] - EMA[1]`) é positiva.
2. A inclinação anterior da EMA (`EMA[1] - EMA[2]`) também é positiva, sinalizando aceleração.
3. O fechamento do candle anterior está acima do valor anterior da EMA.
4. A linha principal do ADX é maior que o limiar configurado.
5. DI+ supera DI-, indicando dominância direcional altista.

Quando todas as regras se alinham e não há posição aberta, a estratégia envia uma ordem de compra a mercado usando o volume configurado. Se existir uma posição vendida, ela é fechada assim que as condições altistas aparecem.

### Entrada vendida
1. A inclinação atual da EMA é negativa.
2. A inclinação anterior da EMA também é negativa.
3. O fechamento do candle anterior está abaixo do valor anterior da EMA.
4. A linha principal do ADX é maior que o limiar.
5. DI- supera DI+, destacando momentum baixista.

Uma ordem de venda a mercado é colocada quando as cinco condições são satisfeitas e a estratégia está zerada. Posições compradas abertas são fechadas imediatamente se os filtros baixistas aparecerem.

### Regras de saída
- **Posições compradas:** sair quando as condições de entrada vendida se materializam, garantindo que o sistema saia das compras quando o momentum do mercado vira para baixo.
- **Posições vendidas:** sair quando as condições de entrada comprada se materializam.
- **Ordens protetoras:** `StartProtection` anexa ordens de stop loss e take profit calculadas a partir do `PriceStep` do instrumento multiplicado pelas distâncias configuradas em pontos. Essas ordens acompanham a posição ativa conforme o mecanismo nativo de ordens protetoras do StockSharp.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `AdxPeriod` | 33 | Número de barras usado no cálculo do ADX. |
| `AdxThreshold` | 22 | Valor mínimo da linha principal do ADX exigido para validar uma tendência. |
| `EmaPeriod` | 39 | Comprimento da EMA usada para detectar inclinação. |
| `StopLossPoints` | 400 | Distância do stop loss medida em pontos do instrumento (multiplicada por `PriceStep`). |
| `TakeProfitPoints` | 900 | Distância do take profit medida em pontos do instrumento. |
| `TradeVolume` | 0.1 | Volume enviado com cada nova ordem a mercado. |
| `CandleType` | Timeframe de 1 hora | Tipo de candle que alimenta todos os cálculos de indicadores. |

## Notas de uso
- Garanta que o ativo forneça um `PriceStep` válido. Quando nenhum passo está disponível, a estratégia usa `1` ponto por padrão para que as ordens protetoras ainda possam ser calculadas.
- Os parâmetros são amigáveis para otimização via `SetCanOptimize(true)`, permitindo backtesting ou otimização com diferentes combinações de EMA/ADX.
- Todos os comentários na implementação C# são escritos intencionalmente em inglês, conforme exigido pelas diretrizes do projeto.
