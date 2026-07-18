# MA em estratégia de lucro mínimo Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica o consultor especialista MetaTrader 5 **MA no Momentum Min Profit.mq5** negociando o cruzamento entre um indicador Momentum e uma média móvel que é calculada no topo da série de momentum. Um sinal de alta aparece quando o momentum ultrapassa sua média, enquanto a barra anterior manteve o momentum abaixo do nível neutro 100. Um sinal de baixa é gerado quando o momentum cruza abaixo da média com a barra anterior acima de 100. A implementação mantém o stop original de capital baseado em dinheiro e a distância fixa de lucro medida em pontos.

## Lógica de negociação
1. Solicite velas definidas por `CandleType` e insira-as no indicador Momentum.
2. Suavize o fluxo de impulso com uma média móvel definida por `MomentumMovingAverageType` e `MomentumMovingAveragePeriod`.
3. Detecte cruzamentos usando os valores da barra anterior para evitar sinais duplos.
4. Recursos opcionais da versão MQL:
   - Inverta a direção dos sinais gerados.
   - Feche a exposição oposta antes de entrar em uma nova negociação ou pule totalmente a entrada.
   - Aplique uma única posição líquida a qualquer momento.
   - Permitir o acionamento na vela atual (em formação) em vez da barra totalmente fechada.
5. Aplicar gerenciamento de risco:
   - Parada de capital em dinheiro: `PnL + Position * (close - PositionPrice)` deve permanecer acima de `StopLossMoney`.
   - Distância de lucro em pontos convertidos por meio de `Security.PriceStep`.

## Parâmetros
| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Velas usadas para calcular o momento. |
| `MomentumPeriod` | `int` | `14` | Período de retrospectiva do indicador Momentum. |
| `MomentumMovingAveragePeriod` | `int` | `6` | Comprimento da média móvel aplicada ao momentum. |
| `MomentumMovingAverageType` | `MomentumMovingAverageType` | `Smoothed` | Algoritmo de média móvel (Simples, Exponencial, Suavizada, Ponderada). |
| `ReverseSignals` | `bool` | `false` | Espelhe MetaTrader sinais de compra/venda. |
| `CloseOpposite` | `bool` | `true` | Feche a exposição oposta antes de abrir uma nova posição. |
| `OnlyOnePosition` | `bool` | `true` | Mantenha uma única posição líquida. |
| `UseCurrentCandle` | `bool` | `false` | Avalie os sinais na vela em formação atual em vez da barra fechada. |
| `StopLossMoney` | `decimal` | `15` | Rebaixamento de capital permitido antes de fechar todas as negociações. |
| `TakeProfitPoints` | `decimal` | `460` | Meta de lucro em pontos de instrumento (multiplicado por `PriceStep`). |
| `MomentumReference` | `decimal` | `100` | Nível de impulso neutro copiado da estratégia MQL. |

## Notas de implementação
- A média móvel é implementada com `LengthIndicator<decimal>` instâncias para reutilizar StockSharp classes SMA/EMA/SMMA/WMA integradas.
- A fila de pedidos original e os filtros de números mágicos são mapeados para StockSharp posições líquidas, portanto, a estratégia envia um único pedido de mercado dimensionado para achatar o lado oposto e abrir a nova exposição quando `CloseOpposite` estiver ativado.
- A proteção de ações fecha todas as posições via `CloseAll()` assim que a perda flutuante ultrapassa o limite, correspondendo exatamente ao comportamento MetaTrader de monitorar a comissão, swap e lucro combinados.
