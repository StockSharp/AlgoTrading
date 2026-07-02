# Macd Pattern Trader Alle v0.01
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert den Expertenberater „MacdPatternTraderAll v0.01“ MetaTrader. Es führt sechs unabhängige MACD-basierte Einstiegsmuster auf demselben Kerzenstrom aus, verwaltet das Risiko mit adaptiven Stop-Loss- und Take-Profit-Levels, führt gestaffelte Gewinnmitnahmen durch und wendet optional eine langsame Martingal-Größenregel nach Verlustzyklen an.

## Kernfunktionen

- **Sechs MACD-Setups** – jedes Muster verwendet seine eigenen schnellen/langsamen EMA-Perioden und Schwellenwerte (`Pattern1` … `Pattern6`). Muster können unabhängig voneinander ein- oder ausgeschaltet werden.
- **Dynamische Risikoniveaus** – Stop-Loss-Niveaus werden von aktuellen Höchst-/Tiefstständen mit konfigurierbaren Offsets abgeleitet, während Take-Profit-Niveaus über aufeinanderfolgende Balkenblöcke iterieren, um die ursprüngliche MQL-Implementierung widerzuspiegeln.
- **Sitzungsfilter** – die Strategie handelt nur innerhalb des konfigurierbaren Fensters `StartTime` / `StopTime`, wenn `UseTimeFilter` aktiviert ist.
- **Teilausstiege** – Profitable Positionen werden in zwei Schritten skaliert, sobald EMA/SMA-Filter die Dynamik bestätigen, und folgen dabei der ursprünglichen `ActivePosManager`-Logik.
- **Langsames Martingal** – wenn `UseMartingale` wahr ist, verdoppelt sich die nächste Handelsgröße nach einem Verlustzyklus und wird nach jedem Gewinnzyklus zurückgesetzt.

## Eingabelogik nach Muster

1. **Muster 1 (Tag `Pattern1`)**
   - Arms a short after the MACD main line pushes above `Pattern1MaxThreshold` and then rolls over with a lower high sequence.
   - Arme lange nach dem Strecken unter `Pattern1MinThreshold` und Erzeugen einer höheren Tieftonsequenz.
2. **Muster 2 (Tag `Pattern2`)**
   - Zählt Schwingungen um die Nulllinie. Shorts werden ausgelöst, wenn ein positiver Swing in der Nähe von `Pattern2MinThreshold` fehlschlägt. Long-Positionen treten auf, wenn ein negativer Schwung in der Nähe von `Pattern2MaxThreshold` nachlässt. Der Algorithmus reproduziert die ursprünglichen Distanzprüfungen durch Vergleich der absoluten MACD-Werte (`valueMin2` / `valueCurr2`).
3. **Muster 3 (Tag `Pattern3`)**
   - Verfolgt bis zu drei absteigende (oder aufsteigende) MACD-Tops, um einen „Dreifachhaken“ zu erkennen. Nur wenn alle Zwischenschwellenwerte (`Pattern3MaxThreshold`, `Pattern3MaxLowThreshold`, `Pattern3MinThreshold`, `Pattern3MinHighThreshold`) übereinstimmen, werden neue Positionen zugelassen.
4. **Muster 4 (Tag `Pattern4`)**
   - Watches for MACD spikes outside `Pattern4MaxThreshold` / `Pattern4MinThreshold` followed by failed attempts to make new extremes. Aus Kompatibilitätsgründen bleibt ein zusätzlicher Zähler (`Pattern4AdditionalBars`) erhalten.
5. **Muster 5 (Tag `Pattern5`)**
   - Implementiert den im Expert Advisor verwendeten Neutralzonen-Breakout. Shorts erfordern einen Rückprall von unter `Pattern5MinThreshold` zurück in die neutrale Zone und einen weiteren Fehlschlag. Longs folgen der gespiegelten Sequenz um `Pattern5MaxThreshold`.
6. **Muster 6 (Tag `Pattern6`)**
   - Zählt die Anzahl aufeinanderfolgender Balken über/unter den Schwellenwerten. Nachdem mehr als `Pattern6TriggerBars` innerhalb des überkauften/überverkauften Bereichs ausgegeben wurden und unter/über dem Schwellenwert zurückgekehrt sind, eröffnet die Strategie einen Handel, es sei denn, `Pattern6MaxBars` blockiert das Signal.

Jedes Muster verwendet die Hilfsmethoden `TryOpenLong` / `TryOpenShort`, um sicherzustellen, dass Stopps und Ziele berechnet werden, bevor ein Auftrag erteilt wird.

## Risiko- und Handelsmanagement

- **Stop-Loss**: `CalculateStopPrice` scannt die letzten `stopBars` abgeschlossenen Kerzen (mit Ausnahme der aktiven) und wendet den konfigurierten Punkt `offset` an. Prices are adjusted for 3/5 decimal instruments just like in the MQL version.
- **Take-Profit**: `CalculateTakeProfit` durchläuft aufeinanderfolgende Blöcke von `takeBars`-Kerzen, bis kein neues Extrem gefunden wird, und ahmt dabei die verschachtelte `iLowest` / `iHighest`-Schleife aus dem Originalcode nach.
- **Teilausstiege**: `ManageActivePositions` schließt ein Drittel der Position mit einem Gewinn von `ProfitThreshold`, wenn der Preis mit `ema2` bestätigt wird. Ein zweiter Exit halber Größe wird ausgelöst, wenn der Preis den kombinierten `(sma3 + ema4) / 2`-Filter erreicht.
- **Hard Exits**: `CheckRiskManagement` gibt vollständige Marktausstiege aus, sobald die gespeicherten Stop-Loss- oder Take-Profit-Level erreicht werden.
- **Martingale-Steuerung**: `OnOwnTradeReceived` akkumuliert realisierte PnL für den aktuellen Flat-to-Flat-Zyklus. Wenn die Position wieder flach ist, setzt `AdjustVolumeOnFlat` entweder das Volumen nach Gewinnen auf `InitialVolume` zurück oder verdoppelt es nach Verlusten, wenn `UseMartingale` aktiviert ist.

## Parameter

Alle Konfigurationsknöpfe werden über `StrategyParam<T>`-Eigenschaften zur Optimierung im StockSharp-Designer verfügbar gemacht.

- **Allgemein**: `CandleType`, `InitialVolume`, `UseTimeFilter`, `StartTime`, `StopTime`, `UseMartingale`.
- **Muster 1–6**: Anzahl der Stop-Loss-/Take-Profit-Balken, Offsets, MACD schnelle/langsame Perioden und Schwellenwerte, die den externen Eingaben aus dem MQL-Skript entsprechen.
- **Positionsmanager**: EMA/SMA Längen (`EmaPeriod1`, `EmaPeriod2`, `SmaPeriod3`, `EmaPeriod4`), die im Teil-Exit-Filter verwendet werden.

Alle Standardwerte spiegeln die `extern`-Variablen von `MacdPatternTraderAll v0.01` wider.

## Nutzungshinweise

- The strategy expects a symbol with a valid `PriceStep` and `Decimals` to compute offsets correctly.
- Stellen Sie eine Kerzenserie über `CandleType` bereit (z. B. `TimeSpan.FromMinutes(5).TimeFrame()`).
- Wenn mehrere Muster gleichzeitig ausgelöst werden, öffnet die Strategie nur eine Position, da jeder Einstiegsaufruf das kombinierte gewünschte Volumen neu berechnet und entgegengesetzte Stopps löscht.
- Die abgestufte Exit-Logik arbeitet mit aggregierten Positionen, sodass es auch dann zu teilweisen Schließungen kommt, wenn mehrere Muster dieselbe Handelsrichtung haben.
