# Estratégia de Gerenciamento de Trailing Straddle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de Gerenciamento de Trailing Straddle** replica o comportamento do Expert Advisor original do MetaTrader 5 "Straddle&Trail". A estratégia coloca um par de ordens stop (um straddle) ao redor do preço atual antes de eventos de notícias programados ou imediatamente sob demanda. Uma vez que uma posição é acionada, o algoritmo gerencia transições de break-even, trailing stops e comandos opcionais de encerramento que cancelam ordens pendentes ou fecham posições abertas.

Esta implementação é construída sobre a API de alto nível do StockSharp. O posicionamento de ordens, o gerenciamento de posições e os controles de risco são implementados sem usar processamento de mensagens de baixo nível.

## Lógica de negociação

1. **Posicionamento do straddle**
   * Duas ordens stop (buy stop acima e sell stop abaixo) são criadas assim que a janela do evento programado é atingida ou instantaneamente se `PlaceStraddleImmediately` estiver habilitado.
   * Os preços das ordens são deslocados do bid/ask atual por `DistanceFromPrice` (expresso em pips). O deslocamento é convertido em unidades de preço usando o passo de preço do instrumento.
   * A estratégia evita recriar o straddle múltiplas vezes no mesmo dia, a menos que as ordens sejam ajustadas ou explicitamente canceladas.

2. **Gerenciamento de ordens pré-evento**
   * Quando `AdjustPendingOrders` está habilitado, as ordens stop são canceladas e reposicionadas a cada novo minuto para que permaneçam alinhadas com o preço atual.
   * Os ajustes param `StopAdjustMinutes` antes do evento para evitar perseguir o preço quando a volatilidade aumenta.
   * Se `RemoveOppositeOrder` estiver habilitado, a ordem stop restante é automaticamente cancelada assim que um lado do straddle é acionado e abre uma posição.

3. **Gerenciamento de risco**
   * Os níveis iniciais de stop-loss e take-profit são calculados a partir de `StopLossPips` e `TakeProfitPips` e são rastreados internamente.
   * Quando o lucro aberto atinge `BreakevenTriggerPips`, o nível de stop é movido para o preço de entrada mais `BreakevenLockPips` (ou o valor simétrico para operações vendidas).
   * Se `TrailPips` for maior que zero, um trailing stop segue o preço. O trailing pode começar imediatamente ou apenas após a condição de break-even, dependendo de `TrailAfterBreakeven`.
   * As saídas de take-profit e stop são executadas com ordens a mercado para confiabilidade.

4. **Encerramento manual**
   * Definir `ShutdownNow` como `true` aciona uma limpeza imediata de acordo com a opção `ShutdownMode`. As ações possíveis incluem fechar posições compradas/vendidas e cancelar ordens pendentes compradas/vendidas.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-----------|
| `ShutdownNow` | Aciona o procedimento de encerramento na próxima atualização de vela. Redefine automaticamente para `false` após a execução. |
| `ShutdownMode` | Define o que deve ser cancelado ou fechado (`All`, `LongPositions`, `ShortPositions`, `PendingLong`, `PendingShort`). |
| `DistanceFromPrice` | Distância entre o preço atual e cada ordem stop, medida em pips. |
| `StopLossPips` | Distância inicial de stop-loss para posições acionadas. Definir como `0` para desabilitar. |
| `TakeProfitPips` | Distância inicial de take-profit. Definir como `0` para desabilitar. |
| `TrailPips` | Distância do trailing stop. Definir como `0` para desabilitar o trailing. |
| `TrailAfterBreakeven` | Quando `true`, o trailing começa apenas após a condição de break-even ser satisfeita. |
| `BreakevenLockPips` | Lucro bloqueado quando o gatilho de break-even é ativado. |
| `BreakevenTriggerPips` | Limite de lucro que ativa a lógica de break-even. |
| `EventHour` / `EventMinute` | Horário do evento programado (horário do broker/servidor). Definir ambos como `0` para desabilitar o agendador de eventos. |
| `PreEventEntryMinutes` | Minutos antes do evento quando o straddle deve ser colocado. Ignorado quando o evento está desabilitado ou quando o posicionamento imediato está habilitado. |
| `StopAdjustMinutes` | Número de minutos antes do evento quando o ajuste automático de ordens pendentes para. |
| `RemoveOppositeOrder` | Cancela a ordem stop não preenchida quando a primeira parte do straddle é acionada. |
| `AdjustPendingOrders` | Habilita o recentramento automático de ordens pendentes enquanto aguarda o evento. |
| `PlaceStraddleImmediately` | Coloca o straddle logo após a estratégia iniciar, ignorando o agendamento de eventos. |
| `CandleType` | Assinatura de velas usada para rastreamento de tempo. Padrão: velas de 1 minuto. |

> **Volume** – a propriedade `Volume` do StockSharp controla o tamanho da ordem. É definida como `1` por padrão e pode ser modificada antes de iniciar a estratégia.

## Assinaturas de dados

A estratégia assina:

* A série de velas configurada (padrão 1 minuto) para executar o agendador, lógica de trailing e verificações de encerramento.
* O livro de ordens para acompanhar os preços mais recentes de bid/ask para alinhamento preciso de ordens stop.

## Notas e limitações

* O gerenciamento de stop-loss e take-profit é executado via ordens a mercado em vez de modificar ordens de proteção do lado do broker. Isso espelha o comportamento original enquanto mantém a implementação simples.
* A estratégia usa o `PriceStep` do instrumento para aproximar o tamanho do pip. Para instrumentos exóticos, ajuste os parâmetros adequadamente.
* O comando de encerramento é avaliado apenas quando novos dados de velas chegam. Para ação imediata, reduza o período das velas.
* A implementação em Python é intencionalmente omitida conforme solicitado.

## Notas de conversão

* A lógica de break-even e trailing é portada linha por linha da versão MQL. A versão StockSharp mantém as mesmas relações numéricas, mas opera com preços decimais e usa saídas a mercado.
* O tratamento manual de trades (magic number `0` em MQL) não é reproduzido porque as estratégias do StockSharp gerenciam suas próprias posições. Toda a lógica de proteção se aplica apenas a trades gerados pela estratégia.
* A função `CalcMagic` é desnecessária no StockSharp e, portanto, foi removida. O estado da estratégia é rastreado internamente pelo framework.

