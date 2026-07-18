# Zeitgesteuerte Kauforder-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Zeitgesteuerte Kauforder-Strategie** repliziert den MetaTrader Expert Advisor `buy_order.mq4`, der einen Strom von Markt-Kauforders über einen Ein-Sekunden-Timer sendet. Der StockSharp-Port behält denselben Handelsrhythmus bei: Er wartet, bis der Timer mit der erwarteten Sekunde innerhalb der aktuellen Minute übereinstimmt, und sendet dann die nächste Order. Nach einer vordefinierten Anzahl von Ausführungen stoppt sich die Strategie automatisch.

Diese Implementierung basiert auf dem High-Level StockSharp-`Timer`-Service anstelle manueller Schleifen. Es sind keine Marktindikatoren oder Kerzen-Abonnements erforderlich, wodurch die Logik deterministisch und zeitorientiert wird.

## Kernlogik
1. Wenn die Strategie startet, aktiviert sie den Risikoschutz über `StartProtection()` und startet einen Timer mit dem konfigurierten Intervall (Standard: eine Sekunde).
2. Jeder Timer-Callback prüft, ob die Strategie online und handelsberechtigt ist, und ob die aktuelle Börsensekunde mit dem erwarteten Sequenzwert übereinstimmt.
3. Wenn alle Überprüfungen erfolgreich sind, sendet die Strategie eine Markt-Kauforder mit dem konfigurierten Volumen.
4. Der Prozess wiederholt sich, bis die Zielanzahl von Orders gesendet wurde, woraufhin die Strategie stoppt.

Das Sekunden-Synchronisationsverhalten spiegelt den ursprünglichen MQL-Experten wider: Die erste Order wird nur gesendet, wenn die Sekundenkomponente null erreicht, und jede folgende Order ist an den nächsten Sekundenwert gebunden.

## Parameter
| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `OrderVolume` | `decimal` | `0.01` | Menge für jede Markt-Kauforder. Eine Validierungssperre stoppt die Strategie, wenn der Wert nicht positiv ist. |
| `OrdersToPlace` | `int` | `60` | Gesamtanzahl der sequenziellen Kauforders, die vor dem Stoppen gesendet werden sollen. |
| `Interval` | `TimeSpan` | `1s` | Verzögerung zwischen Timer-Callbacks. Ein Sekundenintervall reproduziert das MQL-Timing am besten, aber andere Werte sind für Experimente möglich. |

Alle Parameter werden durch StockSharp `StrategyParam<T>`-Objekte bereitgestellt, sodass sie über UI-Tooling optimiert oder konfiguriert werden können.

## Ausführungsablauf
- **Initialisierung** – das Zurücksetzen der Zähler in `OnReseted()` stellt einen sauberen Zustand beim Neustart oder bei der Re-Optimierung sicher.
- **Start** – in `OnStarted()` beginnt der Timer und Zähler werden zurückgesetzt; der Schutz wird einmal pro Lebenszyklus aktiviert.
- **Timer-Tick** – die Methode `OnTimer()` führt die Sequenzierungsüberprüfungen durch, protokolliert die ausgehende Order und stoppt die Strategie, wenn die letzte Order gesendet wird.
- **Abschluss** – der Helfer `CompleteStrategy()` verhindert doppelte Shutdown-Versuche und ruft `Stop()` genau einmal auf.

## Konvertierungshinweise
- Die MQL-Funktion `EventSetTimer(1)` wird auf `Timer.Start(TimeSpan.FromSeconds(1), OnTimer)` abgebildet.
- Order-Kommentare und magische Zahlen aus MetaTrader haben keine direkten Entsprechungen in StockSharp, daher wird stattdessen Logging verwendet, um den Fortschritt zu verfolgen.
- Die Strategie behält das "60 Orders pro Minute"-Konzept bei, indem sie die Sekundenkomponente abgleicht statt Timer-Auslösungen zu zählen.

## Verwendungstipps
1. Weisen Sie das gewünschte Wertpapier und Portfolio vor dem Start der Strategie zu.
2. Passen Sie `OrderVolume` an die Lot-Größe des Instruments und die Broker-Regeln an.
3. Wenn Sie weniger Orders benötigen, reduzieren Sie `OrdersToPlace`; um das sekundenbasierte Tempo vollständig zu deaktivieren, setzen Sie `Interval` auf einen beliebigen Wert und entfernen Sie die Sekundenabgleichung im Code (erweiterte Modifikation).
4. Überwachen Sie die Log-Ausgabe, um Order-Einreichungen zu verfolgen und sicherzustellen, dass die Timer-Ausrichtung wie erwartet funktioniert.

## Einschränkungen
- Die Strategie kauft nur; es gibt keine Ausstiegslogik außer manueller Eingriff oder Schutz-Stops, die vom Broker verwaltet werden.
- Die Order-Platzierung ist durch die Genauigkeit des Timer-Services begrenzt, der von der Verbindung und dem Betriebssystem bereitgestellt wird; große Verzögerungen könnten die Sequenz desynchronisieren.

## Dateien
- `CS/TimedBuyOrderStrategy.cs` – Haupt-C#-Implementierung.
- `README_zh.md` – Chinesische Dokumentation.
- `README_ru.md` – Russische Dokumentation.

Ein Python-Port wird gemäß Projektanweisungen absichtlich weggelassen; erstellen Sie ihn später, wenn erforderlich.
