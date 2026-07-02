# Riesige Einkommensstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „Huge Income“. Der ursprüngliche Roboter sucht nach Intraday-Bewegungen, die sich vom Tageseröffnungskurs weg erstrecken und eine einzelne Position in Richtung des Ausbruchs einnehmen. Die StockSharp-Version behält die gleiche Idee bei, indem sie die tägliche Hoch-/Tiefspanne aus Intraday-Kerzen neu aufbaut, jeweils nur eine Position öffnet und einen Ausstieg kurz vor dem konfigurierten Marktschluss erzwingt.

## Daten und Umgebung
- **Instrumente**: Jedes Symbol, das einen zuverlässigen Preisschritt bietet (`PriceStep`). Die Logik wurde für Forex-Paare entwickelt, funktioniert aber nach Anpassung der Pip-Parameter auch auf anderen Instrumenten.
- **Zeitrahmen**: Standardmäßig abonniert die Strategie 15-Minuten-Kerzen, um den täglichen Eröffnungs-, Höchst- und Tiefststand zu rekonstruieren. Sie können zu einem anderen Kerzentyp wechseln, wenn Ihre Datenquelle eine bessere Auflösung bietet.
- **Sitzungen**: Es wird erwartet, dass die Diagrammzeit genau wie beim MetaTrader-Skript der Broker-/Serveruhr folgt. Stellen Sie die Sperrzeiten entsprechend dieser Zeitzone ein.

## Handelslogik
1. Erstellen Sie die Statistiken des aktuellen Tages neu, wenn eine neue Kerze eintrifft. Die erste Kerze des Tages liefert den Eröffnungspreis und initialisiert das laufende Hoch/Tief.
2. Es ist jeweils nur eine Position (Long oder Short) zulässig. Ausstehende Bestellungen werden nicht verwendet; Die Strategie basiert auf Marktaufträgen.
3. **Lange Einrichtung**:
   - Der aktuelle Schlusskurs liegt über dem Tageseröffnungskurs.
   - Der Abstand zwischen dem Eröffnungskurs und dem aktuellen Tagestief ist größer als `MinimumRangePips` (umgerechnet in Preiseinheiten bis `PriceStep`).
   - Die aktuelle Stunde liegt eindeutig unter `BuyCutoffHour`.
4. **Kurze Einrichtung**:
   - Der aktuelle Schlusskurs liegt unter dem täglichen Eröffnungskurs.
   - Der Abstand zwischen dem Hoch des aktuellen Tages und dem Eröffnungskurs ist größer als `MinimumRangePips`.
   - Die aktuelle Stunde liegt eindeutig unter `SellCutoffHour`.
5. Wenn eines der beiden Setups erfüllt ist, sendet die Strategie eine Marktorder mit der Größe `TradeVolume` und wertet keine neuen Einträge aus, bis die Position wieder flach ist.
6. Nachdem der `MarketCloseHour` erreicht ist, wird jede offene Position mit einer Marktorder geschlossen. Dies spiegelt die MetaTrader-Logik wider, die Geschäfte nahe dem Wochenendschluss liquidiert.

## Risiko- und Geldmanagement
- `TradeVolume` ist die feste Bestellgröße. Im Originalskript gibt es kein Mittelwertbildungs- oder Martingalverhalten, daher behält der StockSharp-Port eine konstante Lautstärke bei.
- Es gibt keine expliziten Stop-Loss- oder Take-Profit-Level. Der Fachberater verlässt sich zur Risikokontrolle auf den täglichen Bereichsfilter und den erzwungenen Schlusskurs gegen Sitzungsende. Sie können die Strategie bei Bedarf erweitern, indem Sie Stopps oder Trailing-Logik hinzufügen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Positionsgröße, die beim Senden von `BuyMarket`- oder `SellMarket`-Bestellungen verwendet wird. |
| `MinimumRangePips` | Mindestabstand (in Pips) zwischen der täglichen Eröffnung und dem entgegengesetzten Extrem, bevor ein Handel zulässig ist. Mit `Security.PriceStep` in einen absoluten Preisunterschied umgerechnet. |
| `BuyCutoffHour` | Letzte Stunde (0–23), in der neue lange Einträge geöffnet werden können. Der Vergleich ist streng (`currentHour < BuyCutoffHour`). |
| `SellCutoffHour` | Letzte Stunde (0–23), in der neue Short-Einträge geöffnet werden können. |
| `MarketCloseHour` | Stunde des Tages, an dem alle offenen Positionen liquidiert werden. Legen Sie den Wert auf 23 fest, um dem ursprünglichen Schließverhalten von EA an Freitagen zu entsprechen. |
| `CandleType` | Zeitrahmen, der zum Abonnieren von Kerzen und zum Rekonstruieren täglicher Statistiken verwendet wird. |

## Unterschiede zur MT4-Version
- StockSharp empfängt Kerzendaten anstelle einzelner Ticks. Wenn der MetaTrader-Feed Ihres Brokers auf Tick-für-Tick-Aktualisierungen basiert, wählen Sie ein ausreichend kleines Kerzenintervall, um die gleiche Reaktionsfähigkeit zu emulieren.
- Der `MinimumRangePips`-Filter wird automatisch deaktiviert, wenn dem Instrument ein `PriceStep` fehlt. In diesem Fall wird jeder Ausbruch über/unter die Eröffnung akzeptiert.
- Alle Geschäfte werden mit Marktaufträgen ausgeführt und sofort bei `MarketCloseHour` abgeflacht, wodurch die `OrderClose`-Schleife des Originalcodes ohne ausstehende Aufträge repliziert wird.

## Anwendungstipps
- Passen Sie den Kerzenzeitrahmen an Ihre bevorzugte Ausführungsgeschwindigkeit an. Kürzere Kerzen verfolgen das tägliche Hoch/Tief genauer, erfordern jedoch mehr Daten.
- Überprüfen Sie die Handelszeiten des Instruments. Wenn der Markt früher als Ihr konfigurierter `MarketCloseHour` schließt, wird der erzwungene Ausstieg am folgenden Handelstag ausgelöst.
- Kombinieren Sie die Strategie mit Schutzmaßnahmen auf Portfolio- oder Kontoebene (z. B. `StartProtection`), wenn Sie Stop-Loss- oder Drawdown-Limits benötigen, die über das ursprüngliche Design hinausgehen.
