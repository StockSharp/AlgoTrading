# Estratégia de ruptura SMC Hilo MaxMin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o comportamento do especialista MetaTrader *SMC MaxMin em 1200*. Na hora terminal especificada, ele coloca um
ordem de compra stop acima da máxima da vela anterior e uma ordem de venda abaixo da mínima da vela anterior. Pedidos pendentes são preenchidos
pela distância mínima de stop da corretora, convertida de pips em unidades de preço do instrumento. Quando ocorre um rompimento, a ordem oposta
é cancelado e a posição aberta é gerenciada através de paradas fixas, metas de lucro e um trailing stop opcional.

Principais diferenças em relação ao código MQL4 original:

- As primitivas de ordem StockSharp (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) substituem as chamadas diretas `OrderSend`.
- As entradas mínimas de distância de stop, stop-loss e take-profit são expressas em pips e convertidas por meio de `Security.PriceStep` para
respeite o tamanho real do tick do instrumento.
- O gerenciamento de trailing stop move a ordem de stop somente quando uma distância lucrativa maior que o trailing buffer é alcançada.
- Toda a lógica é conduzida pela assinatura de vela de alto nível API, portanto, nenhuma varredura direta de histórico ou buffers de indicadores manuais são usados.

## Regras de negociação
1. **Hora de configuração** – quando a hora do terminal for igual a `SetHour`, use a vela concluída anteriormente como referência.
2. **Entrada longa** – coloque um stop de compra em `previous_high + min_stop_distance + price_step`.
3. **Entrada curta** – coloque um sell-stop em `previous_low - min_stop_distance - price_step`.
4. **Exclusividade mútua** – se um dos stop for preenchido, a ordem pendente oposta será cancelada imediatamente.
5. **Stop-loss** – o stop longo é `previous_low - StopLossPips`, o stop curto é `previous_high + StopLossPips` (ambos convertidos
para unidades de preço).
6. **Take-profit** – posições longas usam um limite de venda em `entry + TakeProfitPips`; posições curtas usam um limite de compra em
`entry - TakeProfitPips`.
7. **Trailing stop** – quando uma posição tem lucro superior a `TrailingStopPips`, o stop é seguido para manter o mesmo pip
distância do lance/pedido atual.
8. **Tempo limite do pedido** – duas horas após a configuração (`SetHour + 2`), todas as paradas pendentes não preenchidas serão canceladas.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `Volume` | Volume de pedidos utilizado para ambos os pedidos de entrada. | `0.1` |
| `SetHour` | Hora terminal (0–23) quando o straddle de breakout é criado. | `15` |
| `TakeProfitPips` | Distância alvo de lucro em pips. Defina como `0` para desativar ordens de realização de lucro. | `500` |
| `StopLossPips` | Distância de parada protetora em pips. Defina como `0` para desativar a parada inicial. | `30` |
| `TrailingStopPips` | Distância para o trailing stop em pips. Defina como `0` para manter uma parada estática. | `30` |
| `MinStopDistancePips` | Distância mínima de parada do corretor usada para aumentar os preços de entrada. | `0` |
| `CandleType` | Tipo de vela que define a sessão de hora em hora, o padrão é o período de 1 hora. | `1h` |

## Notas de uso
- A estratégia requer dados de nível 1 para gerenciar os trailing stops e manter os preços de compra/venda mais recentes para cálculos de distância.
- Se o instrumento subjacente tiver tamanhos de ticks fora do padrão (por exemplo, JPY cruza com 0,01 pip), ajuste `TakeProfitPips`,
`StopLossPips` e `TrailingStopPips` respectivamente.
- Quando `TakeProfitPips` ou `StopLossPips` for zero, as respectivas ordens não serão enviadas, mas os trailing stops ainda poderão ser ativados se
o parâmetro final é positivo.
- Certifique-se de que o `SetHour` configurado corresponda ao horário do servidor intermediário do feed de dados de entrada.
