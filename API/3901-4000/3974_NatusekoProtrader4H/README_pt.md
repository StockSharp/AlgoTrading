# Estratégia Natuseko Protrader 4H
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Natuseko Protrader 4H é uma versão StockSharp do consultor especialista MetaTrader 4 *NatusekoProtrader4HStrategy*. O original
o robô combina médias móveis exponenciais, um oscilador MACD filtrado por Bollinger bandas, RSI limites e o Parabolic SAR para
identifique velas de rompimento fortes no período de quatro horas. Quando uma vela qualificada aparece, o sistema abre imediatamente ou
espera por um retrocesso em direção ao EMA rápido antes de entrar. Uma vez posicionada, a estratégia realiza realização parcial de lucros e saídas totais
com base nos sinais RSI e Parabolic SAR, replicando o bloco de gerenciamento de dinheiro presente no código MQL.

## Lógica de negociação
1. Assine o fluxo de velas principal definido por `CandleType` (velas de 4 horas por padrão) e processe apenas velas concluídas.
2. Calcule três médias móveis exponenciais (rápida, lenta e tendência) nos preços de fechamento. Todos os três têm comprimentos configuráveis.
3. Alimente o indicador MACD (períodos rápido, lento e de sinal retirados do EA) e aplique uma média móvel simples mais Bollinger bandas para
a linha principal MACD. A linha média Bollinger atua como o nível de referência usado pela versão MQL.
4. Calcule o RSI nos preços de fechamento e o Parabolic SAR usando dados completos de velas. Esses indicadores impulsionam entradas e saídas.
5. Detecte velas de configuração de alta quando todas as condições a seguir forem válidas:
   - O EMA rápida está acima do lento e da tendência EMA.
   - RSI está acima de `RsiEntryLevel`, mas abaixo de `RsiTakeProfitLong`.
   - A linha principal MACD está acima da linha curta SMA e da linha média Bollinger; o SMA também está acima da linha média.
   - O corpo da vela é maior que ambas as sombras, o que significa que a vela fecha fortemente na direção do movimento.
   - Parabolic SAR fica abaixo do fechamento da vela.
6. Detecte configurações de baixa usando as verificações simétricas (rápidas EMA abaixo, RSI entre `RsiTakeProfitShort` e `RsiEntryLevel`, valores MACD
abaixo da linha média Bollinger, corpo da vela de baixa e SAR acima do fechamento).
7. Se a vela de qualificação estiver muito longe da tendência EMA (distância acima de `DistanceThresholdPoints`), defina um sinalizador pendente e aguarde um
retrocesso. Uma entrada longa é acionada quando o preço atinge o EMA rápido, enquanto RSI e SAR permanecem alinhados com o cenário de alta; o
a entrada curta funciona de forma análoga em retrocessos para o EMA rápida de baixo.
8. Quando nenhum pullback é necessário, a estratégia fecha qualquer exposição oposta e abre uma nova posição com lotes de `TradeVolume`. Stop Loss
o posicionamento segue as regras EA: a primeira preferência é dada ao Parabolic SAR se `UseSarStopLoss` estiver ativado, caso contrário, a tendência
EMA é usado. `StopOffsetPoints` é convertido em distância de preço com a etapa de preço do instrumento e aplicado ao nível de stop.
9. Enquanto uma posição longa está aberta, a estratégia recalcula continuamente o preço stop e gerencia as saídas:
   - Se o preço cair abaixo do stop, toda a posição será fechada.
   - Depois de atingir pelo menos `MinimumProfitPoints` de lucro (em pontos de instrumento) a estratégia pode fechar metade da posição quando o
RSI excede `RsiTakeProfitLong` ou quando o Parabolic SAR ultrapassa o preço (controlado por `UseRsiTakeProfit` e
`UseSarTakeProfit`).
   - Quando o lucro for adequado e RSI cair abaixo de `RsiEntryLevel`, a exposição longa restante será fechada.
10. As posições curtas refletem as mesmas regras com os limites RSI revertidos e SAR cheques invertidos em relação ao preço.

## Gestão de posição
- As saídas parciais acontecem no máximo uma vez por lado comercial. Após fechar metade da posição, a estratégia aguarda a condição de saída total
(RSI cruzando de volta ao nível neutro ou um golpe de stop-loss).
- Os preços de stop-loss são recalculados a cada vela usando o valor Parabolic SAR mais recente ou a tendência EMA para permanecer alinhado com a lógica MQL.
- Quando o tamanho da posição retorna a zero, o estado interno (sinalizadores de entrada pendente, referências de parada e marcadores de saída parcial) é redefinido para que o
a próxima negociação começa de forma limpa.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Prazo de 4 horas | Período primário processado pela estratégia. |
| `TradeVolume` | `decimal` | `0.1` | Volume de pedidos usado para entradas. |
| `FastEmaPeriod` | `int` | `13` | Comprimento do filtro EMA rápida. |
| `SlowEmaPeriod` | `int` | `21` | Comprimento do filtro EMA mais lento. |
| `TrendEmaPeriod` | `int` | `55` | EMA usado para verificações de distância e colocação de stop-loss. |
| `MacdFastPeriod` | `int` | `5` | Comprimento EMA rápido dentro do indicador MACD. |
| `MacdSlowPeriod` | `int` | `200` | Comprimento EMA lento dentro do indicador MACD. |
| `MacdSignalPeriod` | `int` | `1` | Comprimento médio móvel do sinal dentro do indicador MACD. |
| `BollingerPeriod` | `int` | `20` | Número de MACD amostras usadas para calcular Bollinger bandas. |
| `BollingerWidth` | `decimal` | `1` | Multiplicador de desvio padrão para as bandas MACD Bollinger. |
| `MacdSmaPeriod` | `int` | `3` | Comprimento da suavização MACD SMA. |
| `RsiPeriod` | `int` | `21` | Comprimento do indicador RSI. |
| `RsiEntryLevel` | `decimal` | `50` | Limite neutro RSI compartilhado pelas regras de entrada e saída. |
| `RsiTakeProfitLong` | `decimal` | `65` | Nível RSI que permite a realização parcial de lucros para posições longas. |
| `RsiTakeProfitShort` | `decimal` | `35` | Nível RSI que permite a realização parcial de lucros para posições curtas. |
| `DistanceThresholdPoints` | `decimal` | `100` | Distância máxima em pontos do instrumento entre o preço e a tendência EMA antes do atraso da entrada. |
| `SarStep` | `decimal` | `0.02` | Etapa de aceleração para o Parabolic SAR. |
| `SarMaximum` | `decimal` | `0.2` | Aceleração máxima para o Parabolic SAR. |
| `UseSarStopLoss` | `bool` | `false` | Use o Parabolic SAR para derivar a parada de proteção. |
| `UseTrendStopLoss` | `bool` | `true` | Use a tendência EMA para derivar o stop de proteção. |
| `StopOffsetPoints` | `int` | `0` | Compensação adicional (em pontos) adicionada ao preço do stop de proteção. |
| `UseSarTakeProfit` | `bool` | `true` | Habilite saídas parciais quando o preço cruzar Parabolic SAR. |
| `UseRsiTakeProfit` | `bool` | `true` | Habilite saídas parciais quando RSI atingir o limite de lucro. |
| `MinimumProfitPoints` | `decimal` | `5` | Lucro mínimo (em pontos) antes da ativação das regras de realização de lucros parciais ou totais. |

## Diferenças do original EA
- StockSharp negocia posições líquidas. Para emular o comportamento de ticket único de MetaTrader, a estratégia fecha automaticamente o oposto
exposição antes de abrir uma nova negociação na outra direção.
- Auxiliares de gerenciamento de dinheiro são implementados com ordens de mercado em vez de modificar ordens individuais porque StockSharp não gerencia
paradas por bilhete. O efeito corresponde ao EA: uma saída parcial seguida por uma saída final quando o impulso RSI desaparece.
- Os cálculos da distância do preço baseiam-se no instrumento `PriceStep`. Se o título não definir uma etapa de preço, a estratégia assume uma
etapa 1. Ajuste `DistanceThresholdPoints` e `MinimumProfitPoints` adequadamente para instrumentos que usam tamanhos de pontos diferentes.

## Dicas de uso
- Configure `TradeVolume` de acordo com a etapa do lote do instrumento; o construtor também atribui o mesmo valor a `Strategy.Volume` então
métodos auxiliares usam o tamanho esperado.
- Se as negociações atrasarem com muita frequência porque as velas fecham longe da tendência EMA, diminua `DistanceThresholdPoints` ou desative o filtro por
definindo-o como zero.
- É recomendado traçar a estratégia: o código desenha velas, os três EMAs, RSI, Parabolic SAR e MACD Bollinger bandas para que você possa
confirme visualmente a lógica convertida.
- Os parâmetros MACD refletem a combinação incomum de EA (rápido=5, lento=200, sinal=1). Considere otimizá-los antes de ir ao ar
porque um período lento tão amplo produz valores muito suaves, mas atrasados.
