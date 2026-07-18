# Estratégia AbsolutelyNoLagLWMA Canal de Faixa TM Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port direto do especialista MetaTrader "Exp_AbsolutelyNoLagLwma_Range_Channel_Tm_Plus". Ela opera um canal de preços derivado de uma média móvel ponderada linearmente (LWMA) de dupla suavização das máximas e mínimas dos candles. A versão StockSharp mantém o comportamento original: os sinais são avaliados em candles finalizados de um período selecionável, o estado do canal é codificado da mesma forma que o indicador MQL, e o gerenciamento de posição segue a mesma ordem de prioridade (saída por tempo primeiro, saídas por indicador segundo, novas entradas por último).

## Construção do indicador
1. Para cada candle finalizado, as séries de máxima e mínima são alimentadas em uma primeira LWMA. O parâmetro de comprimento é compartilhado entre os fluxos de máxima e mínima.
2. A saída da primeira LWMA é suavizada novamente com outra LWMA do mesmo comprimento. Isso recria a suavização "AbsolutelyNoLagLWMA" usada pelo indicador original.
3. Os valores finais do canal superior e inferior são comparados com o fechamento do candle:
   * Fechamento acima da linha superior → estado de rompimento altista.
   * Fechamento abaixo da linha inferior → estado de rompimento baixista.
   * Fechamento dentro do canal → estado neutro.
4. A estratégia armazena os estados de canal mais recentes. O parâmetro `SignalBar` controla qual índice de barra é verificado para geração de sinais (0 = último candle fechado, 1 = uma barra atrás, etc.), correspondendo à entrada `SignalBar` do programa MQL.

## Interpretação de sinais
* **Entrada comprada** – habilitada por `EnableBuyEntries`. A estratégia busca um rompimento altista na barra indexada por `SignalBar + 1` enquanto a barra em `SignalBar` já retornou para dentro do canal. O comportamento replica o teste original de "rompimento na barra anterior".
* **Entrada vendida** – habilitada por `EnableSellEntries`. Espelha a lógica comprada para rompimentos baixistas.
* **Saída comprada** – habilitada por `EnableBuyExits`. Um rompimento baixista na barra de referência fecha posições compradas existentes, a menos que já tenham sido fechadas pela saída baseada em tempo no candle atual.
* **Saída vendida** – habilitada por `EnableSellExits`. Um rompimento altista na barra de referência fecha posições vendidas abertas, a menos que a saída baseada em tempo já tenha solicitado o fechamento.

## Gerenciamento de operações
* **Volume de ordem** – retirado do parâmetro `OrderVolume`. As ordens de reversão adicionam automaticamente o valor absoluto da posição atual para evitar exposição residual.
* **Stop loss / Take profit** – compensações absolutas opcionais definidas em pontos do instrumento (`StopLossPoints`, `TakeProfitPoints`). Quando positivos, são convertidos em compensações de preço usando o `PriceStep` do instrumento e passados para `StartProtection`.
* **Saída baseada em tempo** – o EA original fecha posições que excedem um tempo de manutenção (`TimeTrade`, `nTime`). No StockSharp isso é tratado por `UseTimeExit` e `HoldingLimit`. A saída é avaliada antes dos sinais do indicador em cada candle finalizado.
* **Temporização de posição** – a estratégia registra o timestamp da última operação que resultou em uma posição comprada ou vendida. Esses timestamps são usados para a saída baseada em tempo.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-----------|
| `Length` | Comprimento de ambas as passagens LWMA que formam o canal. |
| `SignalBar` | Deslocamento da barra examinada para sinais (0 = último candle fechado). |
| `CandleType` | Período usado para o indicador e avaliação de operações. |
| `OrderVolume` | Volume usado ao enviar novas ordens de entrada. |
| `StopLossPoints` | Distância de stop-loss em pontos do instrumento (0 desabilita o stop). |
| `TakeProfitPoints` | Distância de take-profit em pontos do instrumento (0 desabilita o alvo). |
| `EnableBuyEntries` | Permitir ou proibir novas posições compradas. |
| `EnableSellEntries` | Permitir ou proibir novas posições vendidas. |
| `EnableBuyExits` | Permitir que o indicador feche posições compradas. |
| `EnableSellExits` | Permitir que o indicador feche posições vendidas. |
| `UseTimeExit` | Habilitar fechamento de posições após o término de `HoldingLimit`. |
| `HoldingLimit` | Tempo máximo de manutenção antes que a saída por tempo seja acionada. |

## Notas
* O canal é calculado a partir das máximas e mínimas dos candles exatamente como o indicador MQL incluído `AbsolutelyNoLagLwma_Range_Channel`.
* A estratégia ignora candles incompletos e trabalha apenas com dados completos para evitar sinais prematuros.
* Definir `SignalBar` como `0` corresponde à configuração típica do MT5 onde o último candle fechado é analisado. Valores mais altos reproduzem a confirmação retardada usada pelo EA padrão (`SignalBar = 1`).
* Se `PriceStep` não estiver disponível para o instrumento selecionado, as compensações de stop-loss e take-profit são ignoradas, preservando o comportamento das entradas com valor zero no script original.
