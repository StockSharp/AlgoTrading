# Strategie Fibonacci Potential Entries Retracement Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Fibonacci Retracement-Strategie für potenzielle Entries** bildet den MetaTrader-Experten `EA_PUB_FibonacciPotentialEntries` nach. Der Algorithmus wartet auf Live-Kurse der Stufe 1 und platziert dann zwei ausstehende Aufträge um die manuell eingegebenen Fibonacci-Retracement-Niveaus. Wenn das gemeinsame Gewinnziel erreicht ist, skaliert die Strategie jede Position um 50 % und verschiebt den Schutzstopp auf die Gewinnschwelle für die verbleibende Menge.

## Mapping of the original logic
- **Einstiegsaufträge** – Zwei Limitaufträge werden ausgegeben, sobald sowohl der beste Geld- als auch der beste Briefkurs verfügbar sind:
  - *Erste Bestellung*: platziert beim 50 %-Retracement (`P50Level`). Der Stop-Loss ist drei Spreads unterhalb (Bull-Modus) bzw. oberhalb (Bear-Modus) der 61 %-Marke verankert.
  - *Zweite Ordnung*: platziert beim 61 %-Retracement (`P61Level`), wobei der Stop-Loss drei Spreads vom Mittelpunkt zwischen den 61 %- und 100 %-Niveaus entfernt definiert ist.
- **Richtungsbias** – Die ursprüngliche Eingabe `bType` wird zum Parameter `MarketBias` (`Bull` für Kauflimits, `Bear` für Verkaufslimits).
- **Risikoverteilung** – Der erste Trade riskiert immer `0.7%` des Portfolio-Eigenkapitals. Der zweite Handel verbraucht den verbleibenden Teil von `RiskPercent` (`max(RiskPercent - 0.7, 0)`), wobei die von EA verwendete Aufteilung beibehalten wird.
- **Volumenberechnung** – Das Risiko wird über `Portfolio.CurrentValue` (mit Rückgriffen auf `CurrentBalance` und `BeginValue`) zusammen mit der Preisstufe, den Stufenkosten und dem Multiplikator des Instruments in die Positionsgröße übersetzt.
- **Teilweise Gewinnmitnahme** – Wenn der Preis `TargetLevel` überschreitet, sendet jeder ausgeführte Trade eine Marktorder, um die Hälfte seines offenen Volumens zu schließen. Anschließend wird die Stop-Order auf den aufgezeichneten Einstiegspreis verschoben, der mit der `OrderClose` + `OrderModify`-Sequenz von EA übereinstimmt.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `P50Level` | Price assigned to the 50% Fibonacci retracement. |
| `P61Level` | Preis, der dem 61,8 % Fibonacci-Retracement zugeordnet ist. |
| `P100Level` | Dem 100 % Fibonacci-Retracement zugeordneter Preis (wird für den Midpoint-Stopp verwendet). |
| `TargetLevel` | Shared profit target for both trades. |
| `RiskPercent` | Gesamtrisikobudget in Prozent des Eigenkapitals (muss ≥ 0,7 sein). |
| `MarketBias` | Wählt eine lange (`Bull`) oder kurze (`Bear`) Kampagne. |

## Ausführungsdetails
1. Abonnieren Sie Kurse der Stufe 1 über `SubscribeLevel1()` und warten Sie auf positive Geld-/Briefwerte.
2. Compute spread, stop levels and position sizes. Bestellungen werden einmal pro Lauf übermittelt und danach nicht automatisch neu erstellt (gleiches Verhalten wie beim MQL-Experten).
3. Bei Ausführungen zeichnet die Strategie den durchschnittlichen Einstiegspreis auf, platziert die entsprechende Stop-Order und verfolgt das offene Volumen pro Abschnitt.
4. Wenn der Markt über `TargetLevel` hinausgeht, sendet die Strategie eine Teil-Close-Market-Order pro Abschnitt und verschiebt anschließend den Stop auf die Gewinnschwelle für die verbleibende Menge.
5. Stop-Orders werden storniert, wenn kein Volumen mehr vorhanden ist oder die Strategie stoppt.

## Hinweise und Einschränkungen
- Der Stop-Loss wird immer dann neu generiert, wenn sich die Positionsgröße ändert. Wenn der Broker Stop-Orders ablehnt, überprüfen Sie die Connector-Berechtigungen und passen Sie die börsenspezifischen Einstellungen entsprechend an.
- Der Take-Profit wird nicht als ausstehende Order registriert. Stattdessen spiegelt der Algorithmus den EA wider, indem er das Preisniveau überwacht und Exits in Echtzeit verwaltet.
- Da Bestellungen nur einmal erstellt werden, starten Sie die Strategie neu, um ausstehende Orders zu aktualisieren, nachdem sich Parameter geändert haben (identisch mit dem MetaTrader-Workflow).
