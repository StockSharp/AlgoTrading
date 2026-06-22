# Estrategia Kolier SuperTrend X2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia reproduce el experto original de MetaTrader combinando dos filtros SuperTrend que operan en distintos marcos temporales. El SuperTrend del marco temporal superior define el sesgo dominante del mercado, mientras que el SuperTrend del marco temporal inferior busca rupturas sincronizadas para disparar las entradas. El puerto a StockSharp usa vinculaciones de API de alto nivel, por lo que los indicadores reciben actualizaciones de velas directamente y mantienen su propio historial.

## Lógica de trading
- **Filtro de tendencia:** el SuperTrend del marco temporal superior debe confirmar una tendencia alcista o bajista. El retraso de confirmación se controla con `TrendSignalShift`, y el modo (`TrendMode`) define si se requiere una sola barra (`NewWay`) o dos barras consecutivas (todos los demás modos).
- **Señales de entrada:** el SuperTrend del marco temporal inferior espera un cambio de dirección alineado con el filtro de tendencia actual. `EntrySignalShift` retrasa la señal para basarse en barras completamente cerradas, y `EntryMode` controla si la estrategia reacciona de inmediato (`NewWay`) o solo tras una reversión confirmada (otros modos).
- **Entrada largo:** permitida cuando `EnableBuyEntries` es `true`, el filtro de tendencia es alcista y el SuperTrend de entrada cambia a alcista según el modo seleccionado. La exposición corta existente se cierra primero, luego se abre una posición larga con volumen `Volume + |Position|`.
- **Entrada corto:** permitida cuando `EnableSellEntries` es `true`, el filtro de tendencia es bajista y el SuperTrend de entrada cambia a bajista. La exposición larga existente se cubre antes de entrar corto.
- **Salidas:**
  - La reversión del marco temporal superior cierra largos (`CloseBuyOnTrendFlip`) o cortos (`CloseSellOnTrendFlip`).
  - Los cambios en el marco temporal de entrada también pueden cerrar posiciones cuando `CloseBuyOnEntryFlip`/`CloseSellOnEntryFlip` están habilitados.
  - Los stops fijos opcionales (`StopLossPoints`, `TakeProfitPoints`) se aplican como múltiplos de `Security.PriceStep`.

## Indicadores
- Dos instancias de StockSharp `SuperTrend` (una para el marco temporal de tendencia, otra para las entradas).

## Parámetros
- `TrendCandleType` – marco temporal para el filtro de tendencia.
- `EntryCandleType` – marco temporal para las señales de entrada.
- `TrendAtrPeriod`, `TrendAtrMultiplier` – configuración ATR para el SuperTrend de tendencia.
- `EntryAtrPeriod`, `EntryAtrMultiplier` – configuración ATR para el SuperTrend de entrada.
- `TrendMode`, `EntryMode` – modos de confirmación: `NewWay` reacciona tras una barra; otros modos requieren dos barras consecutivas (Visual y ExpertSignal se comportan como el SuperTrend clásico en este puerto).
- `TrendSignalShift`, `EntrySignalShift` – número de barras cerradas a esperar antes de usar los valores del indicador.
- `EnableBuyEntries`, `EnableSellEntries` – habilitar operaciones largas/cortas.
- `CloseBuyOnTrendFlip`, `CloseSellOnTrendFlip` – salir ante señales opuestas del filtro de tendencia.
- `CloseBuyOnEntryFlip`, `CloseSellOnEntryFlip` – salir ante señales opuestas del marco temporal de entrada.
- `StopLossPoints`, `TakeProfitPoints` – distancia en pasos de precio para órdenes protectoras (0 para deshabilitar).
- `Volume` – volumen base para nuevas posiciones.
- `Slippage` – parámetro de marcador de posición mantenido por compatibilidad con el experto original.

## Notas
- El puerto se centra en el flujo de trabajo de alto nivel de StockSharp: las velas se suscriben mediante `SubscribeCandles`, los indicadores se vinculan a través de `BindEx`, y la estrategia mantiene solo el estado mínimo (dirección de tendencia, niveles de stop).
- Se invoca `StartProtection()` una vez para activar el asistente estándar de protección de posiciones de StockSharp.
