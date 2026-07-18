# Estratégia MAMACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão direta do consultor especialista de MetaTrader 5 **MAMACD (edição de barabashkakvn)** da pasta `MQL/19334` para a API de alto nível do StockSharp. A abordagem combina detecção de tendência em preços mínimos através de duas médias móveis ponderadas linearmente (LWMA) com um gatilho de média móvel exponencial (EMA) rápida e confirmação da linha principal do MACD. As negociações são realizadas uma vez por candle finalizado e mantêm a lógica do EA original, incluindo os sinalizadores de reinicialização que exigem que a EMA rápida saia do canal LWMA antes que uma nova entrada seja permitida.

## Indicadores
- **LWMA #1 (preço mínimo, padrão 85)** – filtro de linha de base lento aplicado às mínimas dos candles.
- **LWMA #2 (preço mínimo, padrão 75)** – filtro ligeiramente mais rápido nas mínimas dos candles para confirmação do canal.
- **Gatilho EMA (preço de fechamento, padrão 5)** – gatilho de Momentum que deve cruzar acima/abaixo de ambas as LWMAs para armar uma negociação.
- **Linha principal MACD (rápida 15, lenta 26)** – filtro de confirmação; compras exigem MACD positivo ou ascendente, vendas exigem MACD negativo ou descendente.

## Lógica de entrada
1. A estratégia aguarda apenas candles completados (`CandleStates.Finished`).
2. Quando a EMA gatilho cai abaixo de ambas as LWMAs, um **sinalizador de pronto para comprado** é definido. Uma posição comprada pode ser aberta assim que a EMA retorna acima de ambas as LWMAs **e** o MACD está acima de zero ou maior que seu valor anterior. Apenas uma posição comprada pode ser aberta por vez.
3. Quando a EMA gatilho sobe acima de ambas as LWMAs, um **sinalizador de pronto para vendido** é definido. Uma posição vendida pode ser aberta depois que a EMA retorna abaixo de ambas as LWMAs e o MACD está abaixo de zero ou menor que seu valor anterior. Apenas uma posição vendida está ativa por vez.
4. O dimensionamento da posição usa a propriedade `Volume` da estratégia. Ao mudar de direção, o algoritmo fecha primeiro a exposição oposta.

## Lógica de saída
- Nenhuma lógica de saída discricionária é codificada no EA original. As ordens de proteção são tratadas através do `StartProtection` do StockSharp com distâncias opcionais de stop-loss e take-profit medidas em pips. Atingir qualquer proteção fecha a posição automaticamente.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `FirstLowMaLength` | Período do primeiro LWMA aplicado a preços mínimos (padrão 85). |
| `SecondLowMaLength` | Período do segundo LWMA aplicado a preços mínimos (padrão 75). |
| `TriggerEmaLength` | Período do gatilho EMA rápido em preços de fechamento (padrão 5). |
| `MacdFastLength` | Comprimento da EMA rápida da linha principal MACD (padrão 15). |
| `MacdSlowLength` | Comprimento da EMA lenta da linha principal MACD (padrão 26). |
| `StopLossPips` | Distância de stop-loss em pips; definir como zero para desabilitar (padrão 15). |
| `TakeProfitPips` | Distância de take-profit em pips; definir como zero para desabilitar (padrão 15). |
| `CandleType` | Período dos candles processados pela estratégia (padrão 1 hora). |

## Notas de implementação
- O tamanho do pip é derivado de `Security.PriceStep`. Para símbolos de 3 e 5 dígitos, o código multiplica automaticamente o passo por 10 para imitar a definição de pip do MT5.
- O buffer de histórico do MACD corresponde ao EA: o primeiro valor MACD válido é armazenado e usado como referência para a barra seguinte antes de avaliar os sinais.
- Os sinalizadores `_readyForLong` e `_readyForShort` replicam a máquina de estados `startb`/`starts` original, garantindo que o preço tenha que sair do canal LWMA antes que qualquer nova negociação seja realizada.
- As áreas do gráfico visualizam a série de preços com médias móveis e um painel MACD separado para verificação mais fácil da conversão.

## Mapeamento de conversão
| Elemento MT5 | Equivalente no StockSharp |
| --- | --- |
| `iMA` em mínimo/fechamento | `WeightedMovingAverage` (feed de mínimos) e `ExponentialMovingAverage` (feed de fechamentos) |
| Linha principal `iMACD` | Saída principal de `MovingAverageConvergenceDivergence` |
| Verificações de posição (`buy`, `sell`) | Sinal de `Position` e tratamento de volume via `BuyMarket` / `SellMarket` |
| Número mágico e slippage | Não necessário na API de alto nível do StockSharp |
| Stop-loss / Take-profit (pips) | `StartProtection` com deslocamentos de preço absolutos calculados a partir do tamanho do pip |

O comportamento resultante espelha a versão MT5 enquanto aproveita o ciclo de vida da estratégia, a vinculação de indicadores e os auxiliares de gestão de risco do StockSharp.
