# Pipso Night Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Pipso ist ein Breakout-System für Nachtsitzungen, das vom MetaTrader Expert Advisor `Pipso.mq4` abgeleitet wurde. Die Strategie misst die
den höchsten und niedrigsten Preis der zuvor abgeschlossenen Kerzen und reagiert, wenn der Markt aus diesem Bereich ausbricht. Jeder
Ein Ausbruch kehrt die Position um: Long-Positionen werden geschlossen und eine Short-Position wird eröffnet, wenn der Preis über die jüngsten Höchststände bricht
Short-Positionen werden abgedeckt und eine neue Long-Position wird aufgebaut, wenn der Preis die jüngsten Tiefststände durchbricht. Schutzstopps werden abgeleitet von
die Breite des Bereichs, sodass sich der Stoppabstand automatisch an die aktuelle Volatilität anpasst.

## Wie es funktioniert
1. Abonnieren Sie den konfigurierten Zeitrahmen (standardmäßig 15 Minuten) und warten Sie, bis die Indikatoren einen vollständigen Verlauf erstellt haben.
2. Berechnen Sie für jede neue fertige Kerze das höchste Hoch und das niedrigste Tief der vorherigen `BreakoutPeriod` Kerzen. Der Strom
Kerze ist nicht Teil dieses Bereichs, genau wie im Original EA, wobei `iHighest(..., shift = 1)` den Arbeitsbalken überspringt.
3. Berechnen Sie den Stoppabstand als `(high - low) * StopLossMultiplier` neu und erzwingen Sie dabei den durch definierten Mindestabstand
`MinStopDistance`.
4. Pflegen Sie ein durch `SessionStartHour` und `SessionLengthHours` definiertes Handelsfenster. Wenn das Fenster am Freitag Mitternacht überschreitet
es wird um zwei Tage verlängert, sodass offene Trades das Wochenende überdauern, genau wie in MetaTrader.
5. Wenn das Hoch der Kerze das gespeicherte Ausbruchshoch überschreitet:
   - Schließen Sie alle vorhandenen Long-Positionen und eröffnen Sie, sofern der Handel zulässig ist, eine Short-Position mit der Größe `OrderVolume`.
   - Fügen Sie einen Stop-Loss über dem Einstiegspreis hinzu, indem Sie die berechnete Stop-Distanz verwenden.
6. Wenn das Tief der Kerze unter das gespeicherte Ausbruchstief fällt:
   - Schließen Sie alle vorhandenen Short-Positionen und eröffnen Sie, sofern der Handel zulässig ist, eine Long-Position mit der Größe `OrderVolume`.
   - Fügen Sie einen Stop-Loss unter dem Einstiegspreis hinzu, indem Sie die berechnete Stop-Distanz verwenden.
7. Bei jeder fertigen Kerze werden Schutzstopps bewertet. Wenn das Tief den langen Stopp berührt oder das Hoch den kurzen Stopp erreicht,
die Position wird sofort abgeflacht.

## Handelssitzungslogik
- `SessionStartHour` wird in Wechselstunden ausgedrückt. Die Fensterlänge wird mit `SessionLengthHours` eingestellt.
- Wenn die Sitzung länger als 24 Stunden dauert und der aktuelle Tag Freitag ist, wird das Ende des Fensters um 48 Stunden nach vorne verschoben
dass der Handel am Montag wieder aufgenommen wird, entsprechend der Wochenendbehandlung im Code MQL4.
- Außerhalb des Handelsfensters schließt die Strategie nur bestehende Positionen; Sobald sich das Fenster öffnet, sind neue Trades wieder erlaubt.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Kerzendatentyp, der für die Signalberechnung verwendet wird. | 15-minütiger Zeitrahmen |
| `OrderVolume` | Feste Ordergröße für jede Marktorder. | 1 |
| `SessionStartHour` | Tageszeit, zu der das Breakout-Fenster beginnt. | 21 |
| `SessionLengthHours` | Dauer des Handelsfensters in Stunden. | 9 |
| `BreakoutPeriod` | Anzahl der abgeschlossenen Kerzen, die den Ausbruchsbereich definieren. | 36 |
| `StopLossMultiplier` | Auf die Bereichsbreite angewendeter Multiplikator, um die Stoppentfernung abzuleiten (Wert `3` entspricht dem ursprünglichen `SLpp = 300`). | 3 |
| `MinStopDistance` | Minimaler Stop-Loss-Abstand in absoluten Preiseinheiten, der die Stop-Level-Beschränkung MetaTrader nachahmt. | 0 |

## Notizen
- Die Strategie verwendet nur Marktaufträge; es gibt keinen Take-Profit. Der schützende Stop-Loss ist außerdem der einzige Ausstiegsmechanismus
das entgegengesetzte Ausbruchssignal.
- Beim Wechsel von Long zu Short (oder umgekehrt) sendet die Strategie eine einzelne Marktorder, die beide die vorherige schließt
Position und öffnet die neue, was das Verhalten der Quelle EA widerspiegelt, die nacheinander `OrderClose` und aufgerufen hat
`OrderSend`.
- Indikatorlinien für die Ausbruchshochs und -tiefs werden zusammen mit den ausgeführten Trades automatisch im Strategiediagramm eingezeichnet.
