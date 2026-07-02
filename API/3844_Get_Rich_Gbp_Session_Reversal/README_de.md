# Holen Sie sich eine Rich-GBP-Sitzungsumkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Get Rich or Die Trying GBP-Strategie** ist ein Hochfrequenz-Mean-Reversion-System, das den MetaTrader 4-Expertenberater „Get Rich or Die Trying GBP“ auf die StockSharp-Hochebene API portiert. Die Logik überwacht eine kurze rollierende Historie winziger Kerzen und eröffnet Geschäfte in der Nähe von zwei vordefinierten Tageszeiten, zu denen die letzten Kerzen größtenteils entgegen der erwarteten Richtung geschlossen haben. Dieser Ansatz versucht, ein schnelles Retracement unmittelbar nach der Überschneidung der Londoner und New Yorker Sitzungen zu erfassen.

## Handelslogik
1. Die Strategie abonniert standardmäßig 1-Minuten-Kerzen (der Kerzentyp kann angepasst werden).
2. Ein rollierendes Fenster der letzten fertigen *Lookback*-Kerzen wird beibehalten. Jede Kerze ist kategorisiert als:
   - `+1`, wenn es unter seinem Eröffnungskurs schloss (bärische Kerze).
   - `-1`, wenn es über seinem Eröffnungskurs schloss (bullische Kerze).
   - `0`, wenn die Kerze neutral ist.
3. Die kumulative Summe dieser Klassifizierungen wird zur Entscheidung über die Handelsrichtung verwendet:
   - Eine positive Summe bedeutet, dass bärische Kerzen dominieren und die Strategie sich auf einen **Long**-Einstieg vorbereitet.
   - Eine negative Summe bedeutet, dass bullische Kerzen dominieren und die Strategie sich auf einen **Short**-Einstieg vorbereitet.
4. Bestellungen können nur während der ersten *EntryWindowMinutes*-Minuten nach der Stunde aufgegeben werden, in der die aktuelle Serverzeit mit einer von zwei Zielstunden übereinstimmt:
   - `FirstEntryHour + HourShift` (Standard: London Mitternacht nach der GMT+2-Korrektur).
   - `SecondEntryHour + HourShift` (Standard: 21:00 Uhr Serverzeit für die enge Überschneidung in New York).
5. Wenn keine Position offen ist und alle Bedingungen erfüllt sind, sendet die Strategie eine Marktorder mit entweder der festen Losgröße oder der dynamischen Größe, die aus dem Money-Management-Block berechnet wird.
6. Während man sich in einer Position befindet, wendet die Strategie drei unabhängige Ausstiegsregeln an:
   - Ein **teilweiser Take-Profit** schließt den Handel ab, sobald sich der Schlusspreis zu Ihren Gunsten bewegt.
   - Ein **harter Stop-Loss** wird ausgelöst, wenn sich der Preis gegen den Handel bewegt.
   - Ein **Trailing Stop** sichert den Gewinn, nachdem sich der Markt über die *TrailingStopPoints*-Preisschritte hinaus bewegt hat, wobei das höchste Hoch (für Long-Positionen) oder das niedrigste Tief (für Short-Positionen) seit dem Einstieg verwendet wird.
7. Als Sicherheitsnetz wird auch ein endgültiges Take-Profit-Niveau in Höhe von *TakeProfitPoints*-Preisschritten überwacht.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `TakeProfitPoints` | 100 | Maximale Gewinndistanz (in Preisschritten), überwacht nach der Trailing-Logik. |
| `PartialTakeProfitPoints` | 40 | Primäre Take-Profit-Distanz (in Preisschritten), die den frühen Ausstieg des ursprünglichen EA nachbildet. |
| `StopLossPoints` | 100 | Stop-Loss-Distanz (in Preisschritten). |
| `TrailingStopPoints` | 30 | Trailing-Stop-Distanz (in Preisschritten). |
| `FixedVolume` | 1 | Basisauftragsvolumen in Lots, wenn die Geldverwaltung deaktiviert ist. |
| `UseMoneyManagement` | falsch | Ermöglicht die dynamische Positionsgröße basierend auf dem Kontowert und dem konfigurierten Risiko. |
| `RiskPercent` | 10 | Prozentsatz des Portfoliowerts im Verhältnis zum Risiko pro Trade, wenn das Geldmanagement aktiv ist. |
| `Lookback` | 18 | Anzahl der fertigen Kerzen, die bei der bullischen/bärischen Zählung verwendet werden. |
| `FirstEntryHour` | 22 | Erste Handelsstunde vor der Stundenverschiebungskorrektur. |
| `SecondEntryHour` | 19 | Zweite Handelsstunde vor der Stundenverschiebungskorrektur. |
| `HourShift` | 2 | Für beide Handelszeiten gilt eine Zeitzonenkorrektur. |
| `EntryWindowMinutes` | 5 | Breite des Teilnahmefensters (Minuten ab Beginn der qualifizierenden Stunde). |
| `CandleType` | Zeitrahmen von 1 Minute | Kerzentyp zum Abonnieren; kann durch jeden anderen periodischen Kerzentyp ersetzt werden. |

## Money-Management
Wenn `UseMoneyManagement` aktiviert ist, schätzt die Strategie das Auftragsvolumen, indem sie riskiert, dass `RiskPercent` des aktuellen Portfoliowerts über dem konfigurierten `StopLossPoints` liegt. Bei der Berechnung werden der Losschritt und das Mindestvolumen des Instruments berücksichtigt, um die Börsenkonformität zu gewährleisten.

## Nutzungshinweise
- Die Handelsfenster werden anhand der Börsen-/Serverzeit der eingehenden Kerzen ausgewertet. Passen Sie `HourShift` so an, dass `FirstEntryHour + HourShift` und `SecondEntryHour + HourShift` mit den gewünschten Sitzungsgrenzen übereinstimmen.
- `Lookback` sollte größer als 1 bleiben, um verrauschte Entscheidungen zu vermeiden. Eine Erhöhung glättet die Stimmungsmessung auf Kosten langsamerer Reaktionen.
- Die Schutzlogik setzt auf fertige Kerzen. Wenn Intrabar-Präzision erforderlich ist, reduzieren Sie die Kerzendauer entsprechend.
- Der ursprüngliche MQL-Experte erlaubte mehrere gleichzeitige Positionen; Dieser Port begrenzt die Exposition gegenüber einer einzelnen offenen Position, um den Best Practices von StockSharp zu entsprechen.

## Einschränkungen
- Der Trailing Stop ist virtuell und wird ausgeführt, indem ein Marktausstieg bei der nächsten fertigen Kerze gesendet wird, nachdem der Preis die Trailing-Schwelle überschreitet.
- Beim Money-Management-Sizing wird davon ausgegangen, dass `Security.StepPrice` den Geldwert einer Preisstufe korrekt darstellt. Validieren Sie diese Zuordnung für jedes Instrument vor dem Live-Handel.

## Anforderungen
- StockSharp High-Level-API-Umgebung (AlgoTrading-Lösung).
- Historische und Echtzeit-Minutenkerzen für das gehandelte GBP-Instrument.

## Referenzen
- Ursprünglicher MetaTrader 4 Fachberater: `MQL/7690/Get_rich_or_die_trying_any_gbp.mq4`.
