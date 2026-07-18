# Básico Martingale EA 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **Basic Martingale EA 3** replica o consultor especialista MetaTrader 5 que combina um filtro de tendência baseado na média móvel exponencial tripla (TEMA) com a média martingale orientada por ATR. A versão StockSharp convertida mantém os mesmos parâmetros de risco, janela de negociação e lógica de gerenciamento de dinheiro, ao mesmo tempo que expõe tudo por meio de parâmetros de estratégia para otimização.

## Lógica de negociação
1. **Geração de sinal** – em cada vela concluída do período selecionado, o preço de fechamento é comparado com o valor TEMA. Um fechamento acima do indicador abre uma cesta longa, enquanto um fechamento abaixo dele abre uma cesta curta. Apenas uma direção pode estar ativa ao mesmo tempo.
2. **Janela de negociação** – novas cestas são permitidas somente entre `StartHour` e `EndHour` (horário de troca). Se os dois horários forem iguais a janela é considerada sempre aberta. Defina `TradeAtNewBar` como `true` para limitar novas cestas a uma por vela, semelhante à opção `TradeAtNewBar` original no MT5.
3. **Grade de média** – uma vez existente uma posição, a estratégia mede a distância do pior/melhor preço de entrada. Sempre que o mercado se move pelo menos `GridMultiplier × ATR`, uma ordem adicional é adicionada na direção definida por `Averaging` (média para baixo ou média para cima) até que `MaxAverageOrders` seja alcançado. O novo tamanho do pedido segue o modo martingale escolhido (`Multiply` ou `Increment`).
4. **Saídas de proteção** – os níveis opcionais de stop-loss e take-profit são herdados da primeira ordem na cesta. Além disso, o bloco final imita a implementação MT5: após `TrailingStart` pontos de lucro, o stop é movido para `price - TrailingStop` (ou `price + TrailingStop` para posições vendidas) e reduzido em `TrailingStep`.
5. **Achatamento** – se qualquer nível de stop, take-profit ou trailing for atingido, toda a cesta será fechada no mercado e todos os contadores de média serão zerados.

## Parâmetros
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | Período H1 | Série de velas que impulsiona a estratégia. |
| `StartVolume` | `decimal` | `0.01` | Volume inicial do primeiro pedido em uma cesta. |
| `StopLossPoints` | `decimal` | `20` | Distância de stop-loss em etapas de preço. Defina como `0` para desativar. |
| `TakeProfitPoints` | `decimal` | `20` | Distância de lucro em etapas de preço. Defina como `0` para desativar. |
| `StartHour` | `int` | `3` | Hora (inclusive) em que novas cestas podem começar. |
| `EndHour` | `int` | `18` | Hora (exclusiva) em que a criação do carrinho é interrompida. |
| `TemaPeriod` | `int` | `50` | Comprimento do indicador TEMA. |
| `BarsCalculated` | `int` | `3` | Número de velas concluídas necessárias antes do início da negociação. |
| `AtrPeriod` | `int` | `14` | Período do indicador Average True Range. |
| `GridMultiplier` | `decimal` | `0.75` | Multiplicador ATR que define o espaçamento da grade. |
| `MaxAverageOrders` | `int` | `3` | Número máximo de pedidos médios por direção (incluindo o inicial). |
| `Averaging` | enumeração | `AverageDown` | Escolha entre calcular a média no rebaixamento, calcular a média no lucro ou desativar entradas extras. |
| `Martin` | enumeração | `Multiply` | Selecione entre dimensionamento de martingale multiplicativo ou incremental. |
| `LotMultiplier` | `decimal` | `1.5` | Fator usado pelo modo martingale `Multiply`. |
| `LotIncrement` | `decimal` | `0.1` | Volume adicional usado pelo modo martingale `Increment`. |
| `TradeAtNewBar` | `bool` | `false` | Restrinja as novas cestas a uma por vela acabada. |
| `TrailingStart` | `int` | `100` | Lucre em pontos necessários para ativar o trailing. |
| `TrailingStop` | `int` | `50` | Distância de parada final em pontos. |
| `TrailingStep` | `int` | `30` | Melhoria mínima (pontos) antes de mover o trailing stop novamente. |

## Notas de conversão
- A versão StockSharp mantém a configuração do indicador MT5 (TEMA(50) + ATR(14)) e expõe o parâmetro `bar` como `BarsCalculated`, garantindo pelo menos o número especificado de velas antes da negociação.
- O manuseio de volumes respeita os `MinVolume`, `MaxVolume` e `VolumeStep` do instrumento, portanto, a negociação ao vivo respeita os limites de câmbio, mesmo com passos de martingale fracionários.
- A lógica de trailing segue o comportamento original do ponto de equilíbrio mais o trailing step, mas é implementada com dados de posição agregados porque StockSharp posições são compensadas por instrumento.
- As anotações do gráfico do especialista MT5 não foram portadas porque StockSharp já fornece visualização de ordem e posição nos painéis do gráfico.
