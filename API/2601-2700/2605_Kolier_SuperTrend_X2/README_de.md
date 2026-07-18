# Kolier SuperTrend X2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie reproduziert den ursprünglichen MetaTrader-Experten, indem sie zwei SuperTrend-Filter kombiniert, die auf unterschiedlichen Zeitrahmen arbeiten. Der SuperTrend des höheren Zeitrahmens definiert die dominante Marktausrichtung, während der SuperTrend des niedrigeren Zeitrahmens nach synchronisierten Ausbrüchen sucht, um Einstiege auszulösen. Der StockSharp-Port verwendet High-Level-API-Bindungen, sodass die Indikatoren Kerzenupdates direkt empfangen und ihre eigene Historie verwalten.

## Handelslogik
- **Trendfilter:** Der SuperTrend des höheren Zeitrahmens muss einen Aufwärts- oder Abwärtstrend bestätigen. Die Bestätigungsverzögerung wird durch `TrendSignalShift` gesteuert, und der Modus (`TrendMode`) definiert, ob eine einzelne Kerze (`NewWay`) oder zwei aufeinanderfolgende Kerzen (alle anderen Modi) erforderlich sind.
- **Einstiegssignale:** Der SuperTrend des niedrigeren Zeitrahmens wartet auf einen Richtungswechsel, der mit dem aktuellen Trendfilter übereinstimmt. `EntrySignalShift` verzögert das Signal, um sich auf vollständig geschlossene Kerzen zu stützen, und `EntryMode` steuert, ob die Strategie sofort (`NewWay`) oder erst nach einer bestätigten Umkehr reagiert (andere Modi).
- **Long-Einstieg:** Erlaubt, wenn `EnableBuyEntries` `true` ist, der Trendfilter bullish ist und der Einstiegs-SuperTrend gemäß dem gewählten Modus auf bullish wechselt. Bestehende Short-Positionen werden zuerst geschlossen, dann wird eine Long-Position mit dem Volumen `Volume + |Position|` eröffnet.
- **Short-Einstieg:** Erlaubt, wenn `EnableSellEntries` `true` ist, der Trendfilter bearish ist und der Einstiegs-SuperTrend auf bearish wechselt. Bestehende Long-Positionen werden vor dem Short-Einstieg geschlossen.
- **Ausstiege:**
  - Eine Umkehr im höheren Zeitrahmen schließt Longs (`CloseBuyOnTrendFlip`) oder Shorts (`CloseSellOnTrendFlip`).
  - Richtungswechsel im Einstiegszeitrahmen können ebenfalls Positionen schließen, wenn `CloseBuyOnEntryFlip`/`CloseSellOnEntryFlip` aktiviert sind.
  - Optionale feste Stops (`StopLossPoints`, `TakeProfitPoints`) werden als Vielfache von `Security.PriceStep` angewendet.

## Indikatoren
- Zwei Instanzen des StockSharp `SuperTrend` (eine für den Trend-Zeitrahmen, eine für Einstiege).

## Parameter
- `TrendCandleType` – Zeitrahmen für den Trendfilter.
- `EntryCandleType` – Zeitrahmen für Einstiegssignale.
- `TrendAtrPeriod`, `TrendAtrMultiplier` – ATR-Einstellungen für den Trend-SuperTrend.
- `EntryAtrPeriod`, `EntryAtrMultiplier` – ATR-Einstellungen für den Einstiegs-SuperTrend.
- `TrendMode`, `EntryMode` – Bestätigungsmodi: `NewWay` reagiert nach einer Kerze; andere Modi erfordern zwei aufeinanderfolgende Kerzen (Visual und ExpertSignal verhalten sich wie der klassische SuperTrend in diesem Port).
- `TrendSignalShift`, `EntrySignalShift` – Anzahl geschlossener Kerzen, die gewartet werden soll, bevor Indikatorwerte verwendet werden.
- `EnableBuyEntries`, `EnableSellEntries` – Long-/Short-Trades aktivieren.
- `CloseBuyOnTrendFlip`, `CloseSellOnTrendFlip` – Ausstieg bei entgegengesetzten Signalen des Trendfilters.
- `CloseBuyOnEntryFlip`, `CloseSellOnEntryFlip` – Ausstieg bei entgegengesetzten Signalen des Einstiegszeitrahhmens.
- `StopLossPoints`, `TakeProfitPoints` – Abstand in Preisschritten für Schutzorders (0 zum Deaktivieren).
- `Volume` – Basisvolumen für neue Positionen.
- `Slippage` – Platzhalterparameter, der aus Kompatibilitätsgründen mit dem Quellexperten beibehalten wird.

## Hinweise
- Der Port konzentriert sich auf den High-Level-StockSharp-Workflow: Kerzen werden über `SubscribeCandles` abonniert, Indikatoren werden über `BindEx` gebunden, und die Strategie speichert nur minimalen Zustand (Trendrichtung, Stop-Level).
- `StartProtection()` wird einmal aufgerufen, um den Standard-StockSharp-Positionsschutzhelfer zu aktivieren.
