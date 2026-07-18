# Estratégia MultiTimeframeEmaAlignmentStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
O **MultiTimeframeEmaAlignmentStrategy** é uma porta StockSharp do MetaTrader 4 consultor especialista `1h-4h-1d.mq4` da pasta `MQL/7713`. O robô original alinha médias móveis exponenciais rápidas e lentas em três períodos de tempo e aplica gerenciamento de dinheiro protetor por meio de níveis de stop loss fixo, takeprofit e trailing stop. Esta versão C# segue a mesma ideia de alto nível enquanto aproveita as ligações de indicadores e auxiliares de pedidos de alto nível de StockSharp.

## Lógica de negociação
- A estratégia subscreve três séries de velas simultaneamente: M1 (timeframe do sinal), M5 (filtro de médio prazo) e M30 (confirmação de tendência de timeframe superior).
- Cada série alimenta um par de médias móveis exponenciais (EMA) com comprimentos configuráveis (padrão 8 e 64).
- Uma **configuração otimista** exige que o EMA rápido fique acima do EMA lento em todos os três períodos de tempo. Além disso, o EMA rápida não deve perder impulso (valor atual maior ou igual ao valor anterior e também acima do valor `ShiftDepth` barras atrás).
- Uma **configuração de baixa** exige que o EMA rápido permaneça abaixo do EMA lento em todos os três períodos de tempo, com o EMA rápido diminuindo em momentum.
- As ordens são acionadas no fechamento da vela M1 quando as verificações de alinhamento e impulso são satisfeitas. Sinais longos são permitidos somente quando nenhuma posição longa está aberta (ou uma posição curta existente é fechada primeiro) e vice-versa.

Esta interpretação recria a intenção das condições MT4 com o API de alto nível de StockSharp. As comparações de MQL "MA shift" são emuladas por meio do buffer `ShiftDepth` que rastreia os valores de EMA algumas velas atrás e garante que o impulso seja consistente com a direção de entrada.

## Gestão de risco
- O tamanho da posição é controlado pelo parâmetro `TradeVolume` (padrão 3 lotes como o EA original).
- As distâncias opcionais de stop loss e takeprofit são fornecidas em pips. Eles são convertidos em preços por meio do `PriceStep` do instrumento (volta para `0.0001` quando ausente).
- O trailing stop replica o comportamento do EA, movendo o preço stop para mais perto do mercado sempre que a negociação avança o suficiente.
- Os parâmetros de risco podem ser alternados de forma independente, correspondendo aos sinalizadores `StopLossMode`, `TakeProfitMode` e `TrailingStopMode` do script MQL.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `TradeVolume` | Volume do pedido usado por `BuyMarket` / `SellMarket`. Espelha a entrada `Lots`. | `3` |
| `FastLength` | EMA período para a linha rápida. | `8` |
| `SlowLength` | EMA período para a linha lenta. | `64` |
| `ShiftDepth` | Número de velas históricas usadas para emular as comparações de deslocamento da média móvel MQL. | `3` |
| `UseStopLoss` | Ativa stop loss fixo. | `true` |
| `StopLossPips` | Distância de stop loss expressa em pips. | `75` |
| `UseTakeProfit` | Permite obter lucro. | `true` |
| `TakeProfitPips` | Distância Take Profit expressa em pips. | `150` |
| `UseTrailingStop` | Permite o gerenciamento de trailing stop. | `true` |
| `TrailingStopPips` | Distância final em pips. | `30` |
| `M1CandleType` | Tipo de vela para o período do sinal (padrão 1 minuto). | `1m` |
| `M5CandleType` | Tipo de vela para o filtro de médio prazo (padrão 5 minutos). | `5m` |
| `M30CandleType` | Tipo de vela para o período de tempo superior (padrão 30 minutos). | `30m` |

## Notas de uso
1. Anexe a estratégia a um instrumento e garanta que os dados históricos estejam disponíveis para todos os três períodos para permitir que os buffers EMA sejam preenchidos.
2. O parâmetro `ShiftDepth` deve permanecer pelo menos `2` para que a estratégia possa validar o impulso de curto prazo.
3. Quando `UseTrailingStop` está ativo sem `UseStopLoss`, a lógica final ainda inicializa um valor de parada quando a negociação se move a favor.
4. Como StockSharp é executado no fechamento da vela, os resultados podem diferir ligeiramente da execução tick a tick da versão MT4, especialmente em mercados voláteis. O comportamento central de alinhamento de tendências permanece intacto.

## Notas de conversão
- Os cálculos dos indicadores dependem exclusivamente do mecanismo `Bind` de StockSharp; nenhuma coleta manual de histórico de indicadores é usada.
- O gerenciamento de pedidos é implementado com ajudantes de alto nível (`BuyMarket`, `SellMarket`) e rastreamento interno de preços em vez de chamadas diretas `OrderSend`.
- Notificações por email e controles de deslizamento do script MQL são omitidos porque estão fora do escopo de StockSharp.

## Arquivos
- `CS/MultiTimeframeEmaAlignmentStrategy.cs` – principal implementação da estratégia C#.
- `README_ru.md` – Documentação russa.
- `README_zh.md` – documentação chinesa.
