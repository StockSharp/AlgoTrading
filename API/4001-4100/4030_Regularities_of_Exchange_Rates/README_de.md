# Regelmäßigkeiten der Wechselkursstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese StockSharp-Strategie ist eine originalgetreue C#-Konvertierung des MetaTrader 4 Expert Advisors **Strategy_of_Regularities_of_Exchange_Rates.mq4**. Das System wurde als täglicher Breakout-Straddle konzipiert: Es umschließt den Markt mit Stop-Orders, wenn eine bestimmte Stunde erreicht ist, und hält diese Orders bis zur nächtlichen Handelsschlusszeit aktiv. Jede gefüllte Position wird sowohl durch einen Stop-Loss auf Brokerseite als auch durch einen Intraday-Take-Profit-Watchdog überwacht, sodass Geschäfte nicht über die definierte Handelssitzung hinaus andauern.

Im Gegensatz zu indikatorgesteuerten Systemen konzentriert sich die Logik ausschließlich auf Zeit und Entfernung. Wenn der Zeitplan besagt, dass der Markt bereit sein sollte, misst die Strategie einen festen Offset in Brokerpunkten (Pips) vom aktuellen Geld- und Briefkurs und platziert ein Paar symmetrischer Stop-Orders. Der Code passt die Punktberechnung automatisch an Symbole mit 3- oder 5-stelligen Anführungszeichen an und entspricht damit dem Verhalten der ursprünglichen MQL-Version.

## Handelslogik

1. **Öffnungsstunde** – sobald eine fertige Kerze `OpeningHour` meldet, storniert die Strategie alle verbleibenden ausstehenden Aufträge und setzt einen *Kaufstopp* über dem aktuellen Brief und einen *Verkaufsstopp* unter dem aktuellen Gebot. Der Abstand beträgt `EntryOffsetPoints * point`, wobei der Wert `point` vom Instrument `PriceStep` abgeleitet und für gebrochene Anführungszeichen angepasst wird.
2. **Schutzanordnungen** – unmittelbar nach dem Start aktiviert die Strategie `StartProtection` mit dem konfigurierten `StopLossPoints`. Jeder ausgeführte Trade erhält daher einen Makler-Stop-Loss, der mit dem ursprünglichen EA identisch ist.
3. **Take-Profit-Überwachung** – bei jeder abgeschlossenen Kerze prüft der Algorithmus, ob der aktuelle Gewinn `TakeProfitPoints * point` übersteigt. Wenn ja, wird die Position zum Marktwert geschlossen. Dies spiegelt die ursprüngliche `OrderClose`-Schleife wider, die beendet wurde, als der Gewinn den Schwellenwert erreichte.
4. **Schlussstunde** – wenn die Uhr `ClosingHour` erreicht, schließt die Strategie alle offenen Positionen zwangsweise und storniert die Stop-Orders, um sicherzustellen, dass das Buch für die nächste Sitzung unverändert bleibt.
5. **Täglicher Reset** – ein neuer Stapel ausstehender Aufträge wird nur einmal pro Handelstag gesendet, wodurch Duplikate vermieden werden und gleichzeitig die ursprüngliche Absicht einer einzelnen Einrichtung pro Sitzung respektiert wird.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `OpeningHour` | `9` | Stunde (0–23), in der das Paar Stop-Orders platziert wird. |
| `ClosingHour` | `2` | Stunde (0–23), in der ausstehende Aufträge entfernt und alle offenen Geschäfte abgeflacht werden. |
| `EntryOffsetPoints` | `20` | Abstand in Brokerpunkten vom aktuellen Geld-/Briefkurs zu den Stop-Orders. |
| `TakeProfitPoints` | `20` | Gewinnziel in Brokerpunkten, das einen Marktausstieg auslöst. Auf `0` setzen, um den manuellen Take-Profit zu deaktivieren. |
| `StopLossPoints` | `500` | Entfernung in Brokerpunkten für den über `StartProtection` angeschlossenen Schutzstopp. |
| `OrderVolume` | `0.1` | Volumen jeder Stop-Order. |
| `CandleType` | `30 minute time frame` | Kerzenserien zur Auswertung des Zeitplans. Bei jedem Zeitrahmen ≤ 1 Stunde bleibt das Verhalten im Einklang mit dem MQL-Skript. |

## Konvertierungshinweise

- Der ursprüngliche Fachberater arbeitete an Tick-Ereignissen und verwies direkt auf `Hour()`. In StockSharp hört die Strategie auf fertige Kerzen und verwendet deren Öffnungszeit, wodurch die Einmal-pro-Stunde-Logik erhalten bleibt und gleichzeitig die Repository-Richtlinien für Kerzenzustände eingehalten werden.
- Ausstehende Aufträge werden mit `Security.ShrinkPrice` normalisiert, sodass die generierten Preise immer mit der Tick-Größe des Instruments übereinstimmen.
- Das Stop-Management wird an `StartProtection` delegiert und stellt den von der Plattform generierten Stop-Loss wieder her, den MetaTrader während `OrderSend` angehängt hat.
- Der Code verfolgt das letzte Handelsdatum, um zu vermeiden, dass die gleiche Handelsspanne mehrmals am selben Tag erneut übermittelt wird, was im ursprünglichen EA in Zeitrahmen von weniger als einer Stunde passieren konnte.
- Ausführliche Inline-Kommentare verdeutlichen jeden Schritt des Arbeitsablaufs für zukünftige Wartungsarbeiten oder Experimente.
