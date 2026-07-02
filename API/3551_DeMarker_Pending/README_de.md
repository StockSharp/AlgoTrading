# DeMarker ausstehende Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese StockSharp-Strategie reproduziert das Verhalten des MetaTrader-Expertenberaters „DeMarker Pending 2.5“. Der Bot wertet den DeMarker-Oszillator in einem konfigurierbaren Zeitrahmen aus und platziert beim Überschreiten extremer Niveaus eine ausstehende Order in Ausbruchsrichtung. Die Order kann entweder eine Stop-Order oder eine Limit-Order sein, die um eine feste Anzahl von Punkten ausgeglichen wird. Die optionale Filterung des Handelsfensters und der automatische Ablauf sorgen dafür, dass ausstehende Aufträge an das Verhalten des ursprünglichen Experten angepasst werden.

## Handelslogik
- Abonnieren Sie die ausgewählte Kerzenserie und berechnen Sie den DeMarker-Indikator mit der Periode `DemarkerPeriod`.
- Erkennen Sie Überschneidungen der unteren (`DemarkerLowerLevel`) und oberen (`DemarkerUpperLevel`) Schwellenwerte anhand der aktuellen und vorherigen Werte der fertigen Kerze.
- Wenn die untere Ebene nach oben überquert wird, wird ein langer Aufbau in die Warteschlange gestellt; Wenn die obere Ebene nach unten überquert wird, wird ein kurzer Aufbau in die Warteschlange gestellt.
- Konvertieren Sie Setups in ausstehende Orders zum Preis von `Close ± PendingIndentPoints * PriceStep`, indem Sie je nach `Mode` Stop-Orders im Breakout-Modus oder Limit-Orders für Pullback-Einträge verwenden.
- Fügen Sie der ausstehenden Order Stop-Loss- und Take-Profit-Level hinzu, indem Sie den Einstiegspreis um `StopLossPoints` und `TakeProfitPoints` Punkte verrechnen.
- Stornieren oder verwenden Sie ältere ausstehende Orders gemäß `ReplacePreviousPending` und `SinglePendingOnly` erneut, bevor Sie eine neue registrieren.
- Entfernen Sie ausstehende Orders automatisch, sobald ihre `PendingExpirationMinutes` Lebensdauer abgelaufen ist.
- Ignorieren Sie Signale außerhalb des Intraday-Fensters, wenn `UseTimeWindow` aktiviert ist. Jeder Balken wird nur einmal verarbeitet, sodass pro Balken und Richtung höchstens eine neue Pending Order erstellt wird.

## Auftragsverwaltung
- Alle Einträge werden als ausstehende Orders erstellt (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`).
- Für jede ausstehende Order gelten eigene Stop-Loss- und Take-Profit-Preise, so dass die Position sofort nach der Aktivierung geschützt ist.
- Ausstehende Bestellungen werden bei Ablauf storniert, wenn sie durch neue Setups ersetzt werden oder wenn der Bestellstatus in einen inaktiven Status wechselt (ausgeführt, storniert, abgelehnt).

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Bestellvolumen in Losen. |
| `StopLossPoints` | Abstand zwischen Einstiegspreis und Stop-Loss in Punkten. |
| `TakeProfitPoints` | Abstand zwischen Einstiegspreis und Take-Profit in Punkten. |
| `PendingIndentPoints` | Offset zwischen Marktpreis und der ausstehenden Order. |
| `PendingExpirationMinutes` | Lebensdauer jeder ausstehenden Bestellung in Minuten (0 deaktiviert den Ablauf). |
| `Mode` | Pending-Order-Typ (Stopp für Ausbrüche oder Limit für Pullbacks). |
| `SinglePendingOnly` | Wenn diese Option aktiviert ist, wird verhindert, dass mehr als eine aktive ausstehende Bestellung platziert wird. |
| `ReplacePreviousPending` | Storniert aktive ausstehende Orders, bevor eine neue erteilt wird. |
| `DemarkerPeriod` | Lookback-Periode des DeMarker-Oszillators. |
| `DemarkerUpperLevel` | DeMarker-Schwellenwert, der Verkaufs-Setups auslöst. |
| `DemarkerLowerLevel` | DeMarker-Schwellenwert, der Kauf-Setups auslöst. |
| `CandleType` | Zeitrahmen für das Kerzenabonnement und die Bewertung des Indikators. |
| `UseTimeWindow` | Aktiviert die Intraday-Zeitfilterung. |
| `StartTime` | Beginn des Intraday-Handelsfensters. |
| `EndTime` | Ende des Intraday-Handelsfensters. |

## Notizen
- Der ursprüngliche Experte umfasst ausgefeilte Geldmanagement- und Trailing-Stop-Routinen. Dieser Port behält die Signalgenerierung und die Bearbeitung ausstehender Orders bei, vereinfacht jedoch die Positionsgröße auf einen einzigen festen `Volume`-Parameter.
- StockSharp fügt Stop-Loss- und Take-Profit-Preise zum Zeitpunkt der Auftragsregistrierung hinzu; Je nach Broker müssen Sie möglicherweise überprüfen, ob Stop- und Limit-Orders diese Schutzniveaus unterstützen.
- Stellen Sie stets sicher, dass die punktbasierten Abstände mit dem `PriceStep` des gehandelten Symbols kompatibel sind. Legen Sie `PendingIndentPoints`, `StopLossPoints` und `TakeProfitPoints` auf Werte fest, die den Mindestentfernungsanforderungen des Brokers entsprechen.
