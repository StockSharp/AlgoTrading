# Estratégia My TS15 Moving Average Trailing Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia reproduz o comportamento do consultor especialista **my_ts15.mq5** original, gerenciando ordens de trailing stop em torno de uma posição líquida existente. Uma média móvel ponderada linear (LWMA) orienta a colocação do stop e pode ser substituída por outros métodos de suavização. A lógica continuamente:

* Lê o valor da média móvel de um número configurável de velas concluídas.
* Compara o progresso dos preços com a trilha da média móvel e compensações baseadas em preços.
* Move a ordem de parada de proteção somente quando o novo nível melhora o anterior em pelo menos o passo especificado.
* Opcionalmente, impõe uma distância máxima de perda fixando o stop ou liquidando imediatamente a posição quando o limite é quebrado.

The strategy does not produce entry signals. Destina-se a funcionar em conjunto com outros componentes (manuais ou automatizados) que abrem posições no mesmo título.

## Lógica de negociação

1. Assine a série de velas selecionada e vincule um indicador de média móvel usando o StockSharp API de alto nível.
2. Assim que uma vela terminar, armazene o resultado do indicador e obtenha o valor que está `MaBarsTrail + MaShift` barras atrás da barra atual.
3. Converta as configurações baseadas em pontos em distâncias de preços absolutos usando o tamanho do tick do instrumento.
4. For long positions, choose the lowest of:
   * The moving average minus its offset.
   * O preço atual menos a compensação “no lucro”.
Depois fixe a trilha na distância “em perda” e opcionalmente na perda máxima permitida.
5. For short positions, choose the highest of:
   * The moving average plus its offset.
   * O preço atual mais a compensação “no lucro”.
Depois fixe a trilha na distância “em perda” e opcionalmente na perda máxima permitida.
6. Atualize a ordem de parada somente quando a melhoria exceder `TrailStepPoints` (a menos que seja zero, caso em que todas as melhorias serão aceitas).
7. Se o preço ultrapassar a distância máxima de perda e `EnforceMaxStopLoss` estiver ativado, a estratégia fecha a posição imediatamente.

Todas as entradas de preço usam o preço da vela especificado em `MaPrice`, correspondendo à configuração original MQL onde o indicador é alimentado com a série `PRICE_WEIGHTED`.

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `MaPeriod` | `50` | Comprimento da média móvel usada como backbone móvel. |
| `MaShift` | `0` | Mudança adicional (em barras) aplicada ao amostrar o valor da média móvel. |
| `MaMethod` | `LinearWeighted` | Método de suavização da média móvel (simples, exponencial, suavizada, linear ponderada). |
| `MaPrice` | `Weighted` | Candle price fed to the moving average. |
| `MaBarsTrail` | `1` | Número de barras completadas entre a vela atual e a amostra de média móvel. |
| `TrailBehindMaPoints` | `5` | Distância em pontos mantidos entre o stop e a média móvel. |
| `TrailBehindPricePoints` | `30` | Distância em pontos mantida atrás do preço quando a posição é lucrativa. |
| `TrailBehindNegativePoints` | `60` | Distância em pontos mantida atrás do preço quando a posição está perdendo. |
| `TrailStepPoints` | `0` | Melhoria mínima (em pontos) necessária antes de mover o stop. Zero replica o comportamento “sempre atualizar”. |
| `EnforceMaxStopLoss` | `false` | Se ativado, fixe o stop na perda máxima permitida e liquide a posição quando o preço exceder esse limite. |
| `MaxStopLossPoints` | `100` | Maximum allowed loss distance in points. |
| `ShowIndicator` | `true` | Desenhe a média móvel e os marcadores comerciais no gráfico quando a IU estiver disponível. |
| `CandleType` | `M1` | Candle data type driving the calculations. |

Todas as entradas baseadas em pontos são convertidas em distâncias de preço por meio do tamanho do pip do instrumento calculado em `Security.PriceStep`.

## Notas de conversão

* O especialista MQL atualizou o identificador MA manualmente. A implementação StockSharp usa `BindEx` para processar o indicador sem acessar buffers internos ou chamar `GetValue`.
* Os preços de compra/venda não estão disponíveis diretamente nas velas finalizadas, portanto, os cálculos finais usam o preço da vela selecionado por `MaPrice`. Isso mantém o comportamento consistente porque o script original alimentou o indicador com o mesmo preço ponderado e o comparou com os ticks Bid/Ask.
* `PositionModify` é substituído pelo cancelamento e recriação de ordens de stop de proteção (`SellStop` para longo, `BuyStop` para curto). A estratégia armazena o último nível de parada para imitar os limites finais MetaTrader.
* O fechamento forçado opcional (`pre_init`) segue a lógica original: assim que o mercado ultrapassar `MaxStopLossPoints`, a posição é fechada imediatamente.
* No entry logic has been added; os usuários devem combinar este módulo final com seu próprio provedor de sinal.

## Dicas de uso

1. Anexe a estratégia ao mesmo título que abre as posições.
2. Ajuste as distâncias dos pontos ao tamanho do tick do instrumento (os símbolos Forex geralmente usam valores “pip”, os CFDs podem exigir multiplicadores diferentes).
3. Defina `TrailStepPoints` como um valor positivo para reduzir a rotatividade de pedidos em instrumentos ilíquidos.
4. Desative `EnforceMaxStopLoss` se outro gerenciador de risco já controla distâncias de parada brusca.
5. Mantenha `ShowIndicator` ativado enquanto ajusta os parâmetros para visualizar a média móvel e o comportamento final.
