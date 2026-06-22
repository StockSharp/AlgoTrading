# Strategie des Prozentualen Crossover-Kanal-Systems
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein direkter Port des MetaTrader Expert Advisors *Exp_PercentageCrossoverChannel_System*. Sie verfolgt, wie der Preis mit einem benutzerdefinierten "Percentage Crossover Channel" interagiert, und reagiert, wenn Kerzen nach einem vorherigen Ausbruch wieder in den Kanal zurückkehren. Der Code wurde mit den High-Level-APIs von StockSharp neu geschrieben und bewahrt den ursprünglichen Signalfluss.

## Handelslogik

1. **Indikatoraufbau**
   - Der Percentage Crossover Channel baut eine adaptive Mittellinie auf, die nah am Preis bleibt, aber nicht schneller als ein fester Prozentsatz (`Percent`) driften kann.
   - Obere und untere Bänder werden von der Mittellinie mit demselben prozentualen Abstand abgeleitet.
   - Jede abgeschlossene Kerze wird entsprechend ihrer Beziehung zum Kanal von vor `Shift` Kerzen eingefärbt:
     - Farbe `3` / `4`: Schlusskurs über dem oberen Band (bärischer/bullischer Kerzenkörper).
     - Farbe `0` / `1`: Schlusskurs unter dem unteren Band (bärischer/bullischer Körper).
     - Farbe `2`: Kerze schloss innerhalb des Kanals.

2. **Einstiegs- und Ausstiegsregeln**
   - Die letzte `SignalBar`-Kerze und die unmittelbar vorherige werden ausgewertet (spiegelt den MQL `CopyBuffer`-Aufruf wider).
   - **Bullische Sequenz** (`olderColor > 2`): Der Markt schloss kürzlich über dem Kanal. Wenn die jüngste Kerze wieder hineinbewegt hat (`recentColor < 3`) führt die Strategie durch:
     - Schließt jeden aktiven Short, wenn `SellPositionsClose` aktiviert ist.
     - Öffnet eine Long-Position, wenn keine Trades offen sind und `BuyPositionsOpen` aktiviert ist.
   - **Bärische Sequenz** (`olderColor < 2`): Der Markt schloss kürzlich unter dem Kanal. Wenn die letzte Kerze wieder zurückgekehrt ist (`recentColor > 1`) führt die Strategie durch:
     - Schließt jeden Long, wenn `BuyPositionsClose` aktiviert ist.
     - Öffnet einen Short, wenn keine Trades aktiv sind und `SellPositionsOpen` aktiviert ist.
   - Die Logik wartet daher auf einen Ausbruch gefolgt von einem Wiedereintritt in den Kanal, bevor sie in der Ausbruchsrichtung engagiert.

3. **Risikomanagement**
   - Optionaler Stop-Loss und Take-Profit werden in Preisschritten ausgedrückt und an Kerzenhochs/-tiefs ausgewertet.
   - Wenn eine Schutzorder ausgelöst wird, verlässt die Strategie den Markt und ignoriert neue Einstiege für dieselbe Kerze, was das MQL-Verhalten imitiert, bei dem broker-seitige Stops zuerst den Trade schließen.

## Parameter

| Name | Beschreibung |
| ---- | ----------- |
| `Percent` | Kanalbreite in Prozent. Entspricht dem MQL-Indikator-Input. |
| `Shift` | Anzahl der Kerzen zum Vergleich des Ausbruchs mit historischen Bändern. |
| `SignalBar` | Versatz (in Kerzen) für die Signalauswertung. Ein Wert von 1 bedeutet "vorherige Kerze" wie der ursprüngliche EA-Standard. |
| `BuyPositionsOpen` / `SellPositionsOpen` | Öffnung von Trades in der entsprechenden Richtung aktivieren oder deaktivieren. |
| `BuyPositionsClose` / `SellPositionsClose` | Erzwungenes Schließen von Gegenpositionen bei einem neuen Signal aktivieren oder deaktivieren. |
| `StopLoss` | Stop-Loss-Abstand in Vielfachen von `Security.PriceStep`. Auf null setzen zum Deaktivieren. |
| `TakeProfit` | Take-Profit-Abstand in Preisschritten. Auf null setzen zum Deaktivieren. |
| `CandleType` | Zeitrahmen für die Kerzensubskription. Standard sind Vier-Stunden-Kerzen zur Spiegelung von `PERIOD_H4`. |

## Implementierungshinweise

- Die Indikatorlogik ist inline implementiert, da StockSharp keinen nativen Percentage Crossover Channel bereitstellt. Die Mittellinienberechnungen, Bandableitung und Farbzuweisungen reproduzieren den MQL-Quellalgorithmus Schritt für Schritt.
- Das Positionsmanagement folgt den ursprünglichen Hilfsfunktionen (`BuyPositionOpen`, `SellPositionOpen` usw.), indem gegensätzliche Trades vor dem Öffnen eines neuen geschlossen werden und Einstiege übersprungen werden, wenn eine Gegenposition noch vorhanden ist.
- Geldmanagement, Abweichungsbehandlung und margenmoduspezifische Lot-Dimensionierung aus der ursprünglichen Include-Datei werden nicht repliziert. StockSharp-Benutzer sollten das Strategievolumen über Standard-`Strategy`-Eigenschaften oder die Hosting-Umgebung konfigurieren.
- Stop-Loss-/Take-Profit-Werte werden als *Preisschritte* interpretiert, da MetaTrader-Eingaben in Punkten angegeben werden. Stellen Sie sicher, dass das verbundene Instrument einen gültigen `PriceStep` exposes.

## Verwendungstipps

- Hängen Sie die Strategie an ein Instrument mit zuverlässigen Vier-Stunden-Daten, wenn Sie ein mit MetaTrader identisches Verhalten wünschen. Passen Sie `CandleType` an, um mit Intraday-Betrieb zu experimentieren.
- Da die Einstiegslogik zwei abgeschlossene Kerzen mit gültigen Farbinformationen erfordert, lassen Sie die Strategie mit mindestens `Shift + SignalBar + 1` Kerzen Verlauf aufwärmen.
- Der Kanal reagiert sensibel auf den `Percent`-Input. Kleinere Werte liegen eng am Preis und erhöhen die Handelsfrequenz, während größere Werte sich auf stärkere Ausbrüche konzentrieren.
- Bedenken Sie beim Kombinieren mit Portfolio-Risikokontrollen, dass diese Implementierung höchstens eine Position gleichzeitig öffnet und zwischen Long-, Flat- oder Short-Zuständen wechselt.
