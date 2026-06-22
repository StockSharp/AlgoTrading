# Estratégia Kolier SuperTrend X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reproduz o expert original do MetaTrader combinando dois filtros SuperTrend que operam em períodos distintos. O SuperTrend do período superior define o viés dominante do mercado, enquanto o SuperTrend do período inferior busca rompimentos sincronizados para acionar entradas. O port para StockSharp usa vinculações de API de alto nível, de modo que os indicadores recebem atualizações de candles diretamente e mantêm seu próprio histórico.

## Lógica de negociação
- **Filtro de tendência:** o SuperTrend do período superior deve confirmar uma tendência de alta ou baixa. O atraso de confirmação é controlado por `TrendSignalShift`, e o modo (`TrendMode`) define se uma única barra (`NewWay`) ou duas barras consecutivas (todos os outros modos) são necessárias.
- **Sinais de entrada:** o SuperTrend do período inferior aguarda uma virada de direção alinhada com o filtro de tendência atual. `EntrySignalShift` atrasa o sinal para depender de barras completamente fechadas, e `EntryMode` controla se a estratégia reage imediatamente (`NewWay`) ou apenas após uma reversão confirmada (outros modos).
- **Entrada comprado:** permitida quando `EnableBuyEntries` é `true`, o filtro de tendência está em alta e o SuperTrend de entrada vira para alta de acordo com o modo selecionado. A exposição vendida existente é encerrada primeiro; depois, abre-se uma posição comprada com volume `Volume + |Position|`.
- **Entrada vendido:** permitida quando `EnableSellEntries` é `true`, o filtro de tendência está em baixa e o SuperTrend de entrada vira para baixa. A exposição comprada existente é coberta antes de entrar vendido.
- **Saídas:**
  - A reversão no período superior fecha comprados (`CloseBuyOnTrendFlip`) ou vendidos (`CloseSellOnTrendFlip`).
  - Viradas no período de entrada também podem fechar posições quando `CloseBuyOnEntryFlip`/`CloseSellOnEntryFlip` estão habilitados.
  - Stops fixos opcionais (`StopLossPoints`, `TakeProfitPoints`) são aplicados como múltiplos de `Security.PriceStep`.

## Indicadores
- Duas instâncias do StockSharp `SuperTrend` (uma para o período de tendência, outra para entradas).

## Parâmetros
- `TrendCandleType` – período para o filtro de tendência.
- `EntryCandleType` – período para os sinais de entrada.
- `TrendAtrPeriod`, `TrendAtrMultiplier` – configurações ATR para o SuperTrend de tendência.
- `EntryAtrPeriod`, `EntryAtrMultiplier` – configurações ATR para o SuperTrend de entrada.
- `TrendMode`, `EntryMode` – modos de confirmação: `NewWay` reage após uma barra; outros modos exigem duas barras consecutivas (Visual e ExpertSignal se comportam como o SuperTrend clássico neste port).
- `TrendSignalShift`, `EntrySignalShift` – número de barras fechadas a aguardar antes de usar os valores do indicador.
- `EnableBuyEntries`, `EnableSellEntries` – habilitar operações compradas/vendidas.
- `CloseBuyOnTrendFlip`, `CloseSellOnTrendFlip` – sair com sinais opostos do filtro de tendência.
- `CloseBuyOnEntryFlip`, `CloseSellOnEntryFlip` – sair com sinais opostos do período de entrada.
- `StopLossPoints`, `TakeProfitPoints` – distância em passos de preço para ordens de proteção (0 para desabilitar).
- `Volume` – volume base para novas posições.
- `Slippage` – parâmetro de marcador de posição mantido por compatibilidade com o expert original.

## Notas
- O port foca no fluxo de trabalho de alto nível do StockSharp: candles são assinados via `SubscribeCandles`, indicadores são vinculados via `BindEx`, e a estratégia mantém apenas estado mínimo (direção de tendência, níveis de stop).
- `StartProtection()` é invocado uma vez para ativar o auxiliar padrão de proteção de posições do StockSharp.
