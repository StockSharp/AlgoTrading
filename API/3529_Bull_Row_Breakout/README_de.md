# Bull Row Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Bull Row Breakout-Strategie ist eine C#-Konvertierung des MetaTrader 5 Expert Advisors „BULL row full EA“. Der ursprüngliche Roboter wurde mit einem Blockkonstruktor gebaut und kombiniert Preisaktionsmuster mit Momentumbestätigung. Der StockSharp-Port reproduziert die gleiche Logik in einem einzigen konfigurierbaren Zeitrahmen und hält den Handelskommentar bei Bedarf auf Englisch.

Die Strategie eröffnet **Only-Long-Positionen**, nachdem auf eine Abfolge bärischer Kerzen eine Aufwärtsdynamik und ein Ausbruch über die jüngsten Höchststände folgt. Stochastic-Oszillatorfilter steuern die Impulsstärke, während dynamische Stop-Loss- und Zielniveaus die Risikoeinstellungen der MQL-Version wiederherstellen.

## Signallogik
1. Warten Sie, bis eine neue Kerze geschlossen wird (Ausführung „einmal pro Balken“).
2. Stellen Sie sicher, dass derzeit keine Long-Position offen ist.
3. Erkennen Sie eine bärische Reihe:
   - `BearRowSize` aufeinanderfolgende Kerzen, beginnend bei `BearShift` Balken zurück, müssen bärisch sein.
   - Jeder Kerzenkörper muss mindestens `BearMinBody` Preisschritte umfassen.
   - Die Körperprogression muss den ausgewählten `BearRowMode` erfüllen (normal / größer / kleiner).
4. Erkennen Sie eine bullische Reihe:
   - `BullRowSize` aufeinanderfolgende Kerzen, beginnend bei `BullShift` Balken zurück, müssen bullisch sein.
   - Jeder Kerzenkörper muss mindestens `BullMinBody` Preisschritte umfassen.
   - Der Körperfortschritt muss `BullRowMode` erfüllen.
5. Bestätigung des Ausbruchs: Der Schlusskurs der zuletzt abgeschlossenen Kerze muss höher sein als das höchste aufgezeichnete Hoch von Balken 2 bis zu `BreakoutLookback` Balken.
6. Stochastic Bestätigung:
   - Der aktuelle %K (`StochasticKPeriod`) muss über %D (`StochasticDPeriod`) liegen.
   - Die letzten `StochasticRangePeriod` %K-Werte müssen zwischen `StochasticLowerLevel` und `StochasticUpperLevel` liegen.
7. Risikomanagement:
   - Der Stop-Preis ist das niedrigste Tief unter den letzten `StopLossLookback` Kerzen (beginnend mit dem letzten geschlossenen Balken).
   - Der Take-Profit wird in einer Entfernung platziert, die `TakeProfitPercent` Prozent der Stop-Distanz entspricht.
   - Stopp und Ziel werden bei jeder geschlossenen Kerze überwacht; Wenn eines der beiden Niveaus intrabar erreicht wird, wird die Position bei der nächsten Aktualisierung zum Marktwert geschlossen.

## Parameter
| Parameter | Beschreibung |
| --- | --- |
| `Volume` | Für jeden Eintrag wird ein festes Handelsvolumen verwendet. |
| `CandleTimeFrame` | Zeitrahmen der verarbeiteten Kerzen. |
| `StopLossLookback` | Anzahl der Balken, die zur Berechnung des dynamischen Stop-Preises verwendet werden. |
| `TakeProfitPercent` | Belohnungsdistanz, ausgedrückt als Prozentsatz der Stoppdistanz. |
| `BearRowSize`, `BearMinBody`, `BearRowMode`, `BearShift` | Konfiguration der rückläufigen Reihe, die dem Ausbruch vorausgeht. |
| `BullRowSize`, `BullMinBody`, `BullRowMode`, `BullShift` | Konfiguration der bullischen Zeile, die dem Signal unmittelbar vorausgeht. |
| `BreakoutLookback` | Länge des gleitenden Hochs, das zur Bestätigung des Ausbruchs verwendet wird. |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Stochastic Oszillatoreinstellungen. |
| `StochasticRangePeriod` | Anzahl der historischen Stochastic-Werte, die innerhalb der Grenzen bleiben müssen. |
| `StochasticUpperLevel`, `StochasticLowerLevel` | Oszillatorkanalgrenzen werden auf %K angewendet. |

Alle Körpergrößen werden in Preisschritten ausgedrückt, um den `toDigits`-Helfer aus dem Originalcode widerzuspiegeln. Wenn das Instrument keinen Preisschritt bietet, wird ein Standardwert von 1 verwendet.

## Unterschiede zur MQL-Version
- Das MT5-Projekt erlaubte separate Zeitrahmen für die Blockeingaben. Der StockSharp-Port arbeitet in einem durch `CandleTimeFrame` definierten Zeitrahmen, der der allgemeinen Verwendung des ursprünglichen EA entspricht (alle Blöcke im Zeitrahmen des Diagramms).
- Virtuelle Stopps und die Bearbeitung ausstehender Aufträge aus der generischen Blockbibliothek sind nicht erforderlich und werden daher weggelassen.
- Schützende Stop-Loss- und Take-Profit-Levels werden durch die Überwachung von Kerzen und das Schließen der Position mit `SellMarket` emuliert, sobald ein Level durchbrochen wird.
- Protokollierung und Diagrammdekorationen aus der Umgebung MQL werden nicht repliziert.

## Nutzungstipps
- Optimieren Sie die Zeilengrößen und Verschiebungen für das gehandelte Instrument. Die Standardwerte ahmen die ursprüngliche Voreinstellung nach (drei bärische Kerzen, die drei Balken zurück beginnen, gefolgt von zwei bullischen Kerzen, die einen Balken zurück beginnen).
- Passen Sie `StochasticLowerLevel` und `StochasticUpperLevel` an, um festzulegen, wie restriktiv der Oszillatorfilter sein soll.
- Da der Stop auf aktuellen Tiefstständen basiert, kann es bei Instrumenten mit großen Lücken erforderlich sein, den Lookback zu erweitern oder zusätzliche Filter hinzuzufügen.
