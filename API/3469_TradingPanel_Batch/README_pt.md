# Estratégia de lote do painel de negociação
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
`TradingPanelBatchStrategy` é uma versão StockSharp do MetaTrader 4 consultor especialista **EA_TradingPanel**. O script original expunha um painel manual onde o trader configurava o número de negociações simultâneas, tamanho do lote e distâncias de proteção antes de pressionar **COMPRAR** ou **VENDER**. Na versão StockSharp o mesmo comportamento é automatizado: uma vez que o operador define o parâmetro `Direction`, a estratégia dispara um lote de ordens de mercado na próxima vela concluída e redefine instantaneamente a direção de volta para `None`.

A lógica é intencionalmente simples para que o módulo possa ser combinado com sinais externos ou supervisão manual. Todas as ordens herdam distâncias opcionais de stop-loss e take-profit medidas em pips, refletindo os controles de risco disponíveis na implementação MQL.

## Fluxo de trabalho
1. Quando a estratégia é iniciada, ela calcula o tamanho do pip de `Security.PriceStep`. Para símbolos Forex de 1/3/5 dígitos, o valor é multiplicado por dez, correspondendo à conversão MetaTrader entre pontos e pips.
2. Se as compensações de stop-loss ou take-profit forem diferentes de zero, a estratégia permite que `StartProtection` gerencie saídas com ordens de mercado.
3. A estratégia assina a série de velas especificada por `CandleType`. Após cada vela finalizada ele verifica o parâmetro `Direction`.
4. Se uma direção for solicitada e o mecanismo permitir a negociação, a estratégia envia `NumberOfOrders` ordens de mercado usando `OrderVolume` para cada ticket.
5. Depois que o lote é despachado, a estratégia registra a ação e define automaticamente `Direction` de volta para `None`, pronta para o próximo acionamento manual.

Este design mantém o módulo sem estado entre as execuções. Os traders podem definir repetidamente `Direction` como `Buy` ou `Sell` sempre que precisarem de um novo lote de pedidos; a execução sempre acontece na próxima vela concluída para evitar atuar em dados de mercado parcialmente formados.

## Parâmetros
| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `NumberOfOrders` | `int` | `1` | Número de ordens de mercado enviadas no próximo lote. |
| `OrderVolume` | `decimal` | `0.01` | Volume aplicado a cada ordem de mercado. |
| `StopLossPips` | `decimal` | `2` | Distância de stop-loss convertida de pips em preço absoluto usando os metadados atuais do instrumento. Defina como `0` para desativar. |
| `TakeProfitPips` | `decimal` | `10` | Distância de lucro em pips. Defina como `0` para desativar. |
| `Direction` | `TradeDirection` | `None` | Orientação solicitada para a próxima execução. A estratégia zera o valor após os pedidos serem feitos. |
| `CandleType` | `DataType` | `TimeFrameCandle(1m)` | Série de velas usada para acionar a execução. |

## Notas
- A estratégia requer um `Security` válido com `PriceStep` configurado corretamente (e opcionalmente `Decimals`). Sem esses metadados, os cálculos do pip voltam para `1`.
- `StartProtection` usa ordens de mercado para saídas para imitar como o painel MQL fechou posições em níveis de stop-loss ou take-profit.
- Como a execução ocorre em velas finalizadas, os traders podem sincronizar lotes de pedidos com análises personalizadas ou sinais externos, atualizando `Direction` antes do fechamento da vela.
