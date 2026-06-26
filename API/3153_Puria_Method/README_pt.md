# Estratégia de Puria Method
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Puria Method é um sistema seguidor de tendência originalmente projetado para o MetaTrader. Combina três médias móveis com um filtro de tendência MACD para detectar rompimentos de Momentum. A conversão para StockSharp mantém a lógica de entrada original e adiciona controles de risco modernos, como tomada parcial de lucros e stops de trailing automatizados.

## Lógica de trading
- Calcular três médias móveis usando métodos de suavização e fontes de preço configuráveis.
- Avaliar a diferença entre a MA de referência mais lenta e as duas MAs mais rápidas na barra anterior. Um sinal de alta requer que ambas as MAs rápidas estejam pelo menos 0,5 pontos acima da referência; um sinal de baixa requer que a referência lidere pelo mesmo margem.
- Confirmar a direção do mercado com a linha principal do MACD. Operações longas requerem que o valor anterior do MACD seja positivo e que o histórico recente do MACD seja não-decrescente pelo número configurado de barras. Operações curtas requerem as condições opostas.
- Quando uma entrada é acionada, a estratégia fecha uma posição oposta (se houver) e abre uma nova posição líquida na direção do sinal.

## Gestão de risco
- **Stop Loss / Take Profit:** Os preços são calculados a partir da entrada usando distâncias em pips e normalizados para o passo de preço do instrumento.
- **Trailing Stop:** Uma vez que a posição avance além do limiar de trailing mais o passo, o stop é avançado a cada passo de trailing adicional.
- **Saída parcial:** Após o preço percorrer uma distância mínima de lucro, uma fração configurável da posição é fechada para garantir ganhos.
- **Gestão de posição:** O algoritmo acompanha o preço mais alto (longo) ou mais baixo (curto) após a entrada para acionar regras de stop ou lucro quando as velas ultrapassam esses níveis.

## Parâmetros
| Nome | Descrição |
| ---- | ----------- |
| `StopLossPips` | Distância do stop loss em pips. |
| `TakeProfitPips` | Distância do take profit em pips. |
| `TrailingStopPips` | Distância do trailing stop em pips. |
| `TrailingStepPips` | Avanço mínimo de lucro antes de atualizar o trailing stop. |
| `MinProfitStepPips` | Distância mínima em pips antes de tomar lucro parcial. |
| `MinProfitFraction` | Fração da posição a fechar quando o passo mínimo de lucro é atingido. |
| `CandleType` | Série de velas primária usada pela estratégia. |
| `Ma0Period`, `Ma1Period`, `Ma2Period` | Períodos para as três médias móveis. |
| `Ma0Shift`, `Ma1Shift`, `Ma2Shift` | Deslocamentos de barra opcionais aplicados a cada média móvel. |
| `Ma0Method`, `Ma1Method`, `Ma2Method` | Métodos de suavização de médias móveis (simples, exponencial, suavizado, linear ponderado). |
| `Ma0Price`, `Ma1Price`, `Ma2Price` | Fontes de preço de vela para as médias móveis. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuração do MACD. |
| `MacdTrendBars` | Número de barras para verificar a tendência monotônica do MACD (mínimo 3). |
| `MacdPrice` | Fonte de preço de vela para o cálculo do MACD. |

## Notas
- A estratégia usa a barra concluída anterior para comparações de MA e MACD para evitar depender de dados de velas incompletos.
- O tamanho do pip é derivado automaticamente do passo de preço do instrumento e da precisão decimal.
- As funções de trailing e saída parcial requerem valores de configuração diferentes de zero; caso contrário, os blocos correspondentes permanecem inativos.
- A versão convertida depende exclusivamente de velas concluídas (`CandleStates.Finished`) e deve ser usada com uma série de velas que corresponda ao período de tempo do gráfico original.
