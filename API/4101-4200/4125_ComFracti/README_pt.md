# Estratégia ComFracti
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

ComFracti é uma estratégia direcional traduzida do consultor especialista MT4 "ComFracti". A lógica combina confirmação fractal de vários períodos de tempo com RSI e filtros estocásticos, enquanto filtros opcionais de média móvel, parabólico SAR, canal e perceptron controlam o alinhamento de tendência. A implementação C# negocia uma única posição por vez e avalia sinais em velas concluídas usando StockSharp APIs de alto nível.

## Lógica de negociação

- **Sinal primário**
  - Confirma uma configuração de alta quando o período atual e o período superior produzem um sinal fractal de alta.
  - Confirma uma configuração de baixa quando ambos os intervalos de tempo produzem um sinal fractal de baixa.
  - RSI (3 períodos padrão no período mais alto) deve ficar abaixo de `50 - RsiLevelBuy` para posições longas ou acima de `50 + RsiLevelSell` para posições curtas quando o filtro RSI estiver ativado.
  - O oscilador estocástico (período %K 5 padrão com suavização %D 3/3) deve estar abaixo de `50 - StochasticLevelBuy` para posições compradas ou acima de `50 + StochasticLevelSell` para posições vendidas quando o filtro estocástico estiver ativado.
- **Filtros opcionais**
  - **EMA inclinação**: o EMA no período de tempo do filtro deve estar subindo para posições compradas e caindo para posições vendidas.
  - **Parabolic SAR**: o valor SAR deve ficar abaixo da barra aberta para posições compradas ou acima para posições vendidas.
  - **Quebra de canal**: compara a barra anterior com um canal adaptativo no estilo Donchian; Os mínimos anteriores devem permanecer acima do piso do canal para posições compradas, enquanto os máximos anteriores devem permanecer abaixo do teto para posições vendidas.
  - **Perceptron**: uma soma ponderada das diferenças recentes entre máximos e mínimos deve ser positiva para posições compradas e negativa para posições vendidas.
- **Gerenciamento de posição**
  - Apenas uma posição está ativa por vez; a estratégia fecha a exposição existente antes de abrir uma nova negociação na direção oposta.
  - As distâncias fixas de stop-loss e take-profit são expressas em pontos de instrumento.
  - Um trailing stop opcional se move na direção do lucro quando o buffer móvel é atingido (quando `ProfitTrailing` é verdadeiro).
  - Quando `CloseOnOppositeSignal` está ativado, a estratégia sai mais cedo se o sinal primário oposto aparecer.

## Gestão de risco

- O tamanho da posição base é igual ao parâmetro `BaseVolume` (padrão 0,1 lote). Quando `AccountMicro` está ativado, o volume é dividido por dez.
- Se `UseMoneyManagement` estiver ativado, a estratégia arrisca `RiskPercent` do valor da conta por negociação, usando a distância de stop-loss configurada e o valor do passo do instrumento para aproximar o tamanho da posição. O volume calculado é limitado por `MinimumVolume`.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `TakeProfitPoints`, `StopLossPoints` | Distâncias de take-profit e stop-loss em pontos de instrumento. |
| `UseTrailingStop`, `TrailingStopPoints`, `ProfitTrailing` | Controles de trailing stop (distância e se o trailing requer lucro aberto). |
| `BaseVolume`, `UseMoneyManagement`, `RiskPercent`, `AccountMicro`, `MinimumVolume` | Configuração de dimensionamento de posição. |
| `UseFractals`, `FractalShift*` | Ativa a confirmação fractal e define os deslocamentos da barra para inspecionar nos prazos atuais e superiores. |
| `UseRsi`, `RsiLevelBuy`, `RsiLevelSell`, `RsiType` | RSI filtros de compensação e prazo. |
| `UseStochastic`, `StochasticPeriod*`, `StochasticLevel*` | Stochastic períodos e limites do oscilador. |
| `UseMaFilter`, `MaPeriod` | EMA configuração de filtro no período de filtro. |
| `UsePsarFilter`, `PsarStep` | Parabolic SAR configuração de filtro. |
| `UseChannelFilter`, `ChannelLookback`, `ChannelK` | Parâmetros de filtro de ruptura de canal. |
| `UsePerceptronFilter`, `PerceptronV1`–`PerceptronV4` | Pesos do filtro Perceptron (0–100, centrado em 50). |
| `CandleType`, `HigherFractalType`, `FilterType` | Prazos de dados usados pela estratégia. |

## Notas

- A estratégia processa apenas velas finalizadas, portanto o comportamento pode diferir ligeiramente do consultor especialista original orientado por ticks.
- O rastreador fractal reproduz a lógica fractal de cinco barras do MT4 e permite ao usuário alterar qual barra histórica é avaliada, correspondendo aos parâmetros `sh1/ sh2` do MT4.
- A gestão de dinheiro depende da avaliação do portfólio disponível em StockSharp; quando nenhuma avaliação está disponível, a estratégia volta ao volume base fixo.
