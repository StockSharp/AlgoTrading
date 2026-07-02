# STO M5xM15xM30 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine originalgetreue C#-Konvertierung des MetaTrader 4 Expert Advisors „STO_m5xm15xm30“. Es verwendet drei stochastische Oszillatoren, die auf den Zeitrahmen M5, M15 und M30 berechnet wurden, um synchronisierte Impulsverschiebungen zu identifizieren. Die StockSharp-Implementierung behält die ursprüngliche Ein-/Ausstiegsstruktur bei, ersetzt die manuelle Auftragsverwaltung durch die übergeordnete API und stellt jede Schlüsselkonstante als konfigurierbare `StrategyParam` bereit.

## Handelslogik
1. **Bestätigung mehrerer Zeitrahmen**
   - Die primäre Stochastik (Standard M5) muss einen bullischen Crossover aufweisen (`%K` kreuzt über `%D`).
   - Die mittleren (Standard M15) und langsamen (Standard M30) stochastischen Werte müssen bereits bullisch sein (`%K` über `%D`).
   - Ein bärisches Setup erfordert die gespiegelten Bedingungen (`%K` unter `%D`).
2. **Filter verschieben**
   - Die primäre Stochastik überprüft auch den Status `ShiftBars` Kerzen früher. Für ein Kaufsignal muss der historische `%K` unter `%D` liegen, um einen erneuten Crossover sicherzustellen. Verkaufssignale erfordern das Gegenteil.
3. **Preismomentum-Filter**
   - Der letzte Schlusskurs muss höher (für Käufe) oder niedriger (für Verkäufe) sein als der zuvor abgeschlossene Kerzenschluss. Dies spiegelt die `Close[0] > Close[1]`-Regel aus dem MT4-Skript wider.
4. **Eintrittsregeln**
   - Wenn keine Position offen ist und die bullischen Kriterien erfüllt sind, eröffnet die Strategie eine Long-Market-Order mit dem konfigurierten `TradeVolume`.
   - Wenn beim Eintreffen eines bullischen Signals eine Short-Position besteht, wird diese zunächst abgeflacht und anschließend eine Long-Position eröffnet. Das Umgekehrte gilt für bärische Signale.
5. **Ausgangsregeln**
   - Eine dedizierte M5-Stochastik mit der Periode `ExitKPeriod` prüft die vorherige Kerze (`shift = 1`). Eine Long-Position wird geschlossen, wenn `%K` unter `%D` fällt; Ein Short wird geschlossen, wenn `%K` über `%D` steigt.
   - Nachdem ein Ausstieg ausgelöst wurde, überspringt die Strategie den sofortigen Wiedereintritt bei derselben Kerze und reproduziert so das Verhalten der MT4-Orderschleife.

## Indikatoren und Datenabonnements
- Primärkerzen: Standardzeitrahmen von 5 Minuten (`CandleType`).
- Mittlere Bestätigungskerzen: Standardzeitrahmen von 15 Minuten (`MiddleCandleType`).
- Langsame Bestätigungskerzen: Standardzeitrahmen von 30 Minuten (`SlowCandleType`).
- Stochastic-Oszillatoren: Alle verwenden %K-Glättung = 3 und %D-Glättung = 3, entsprechend den ursprünglichen Parametern.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CandleType` | 5-Minuten-Kerzen | Arbeitszeitrahmen für Ein- und Ausstiege. |
| `MiddleCandleType` | 15-Minuten-Kerzen | Bestätigungszeitraum Nr. 1. |
| `SlowCandleType` | 30-Minuten-Kerzen | Bestätigungszeitraum Nr. 2. |
| `FastKPeriod` | 5 | %K-Periode für die primäre Stochastik. |
| `MiddleKPeriod` | 5 | %K-Periode für die mittlere Stochastik. |
| `SlowKPeriod` | 5 | %K-Periode für die langsame Stochastik. |
| `ExitKPeriod` | 5 | %K-Periode für den Exit-Stochastic, der auf dem vorherigen Balken wirkt. |
| `ShiftBars` | 3 | Anzahl der Balken zwischen dem Referenz-Crossover und dem aktuellen Balken. |
| `TakeProfitPoints` | 30 | Schutz-Take-Profit-Distanz (Punkte). |
| `StopLossPoints` | 10 | Schutz-Stop-Loss-Distanz (Punkte). |
| `TradeVolume` | 0,1 | Bestellvolumen, das für Neuzugänge verwendet wird. |

Alle Parameter werden über `StrategyParam<T>` verfügbar gemacht, sodass sie für die Optimierung im StockSharp-Designer verfügbar sind.

## Risikomanagement
`StartProtection()` übersetzt die MT4-Eingaben `TP` und `SL` in StockSharp Schutzanordnungen. Beide können deaktiviert werden, indem der entsprechende Parameter auf Null gesetzt wird.

## Implementierungshinweise
- Indikatorwerte werden ausschließlich über `SubscribeCandles(...).BindEx(...)` ermittelt, wobei die übergeordneten API-Richtlinien eingehalten und manuelle Indikatorerfassungen vermieden werden.
- Der `StochasticShiftBuffer`-Helfer ahmt das MT4-Argument `shift` nach, ohne `GetValue` aufzurufen, und behält nur den erforderlichen Balkenverlauf bei.
- Die Eingabeverarbeitung erfolgt einmal pro abgeschlossener Kerze. Die Exit-Bewertung erfolgt vor der Eingangslogik und entspricht der Verarbeitungsreihenfolge des ursprünglichen EA.
- Inline-Kommentare erläutern jeden Verarbeitungsschritt und verdeutlichen, wie die MQL-Logik dem StockSharp-Code zugeordnet wird.

## Nutzung
1. Fügen Sie die Strategie einem StockSharp-Schema oder Designerprojekt hinzu.
2. Konfigurieren Sie das gewünschte Symbol und stellen Sie sicher, dass historische Daten für M5-, M15- und M30-Kerzen verfügbar sind.
3. Passen Sie die Parameter an den Zielmarkt oder das Optimierungsszenario an.
4. Starten Sie die Strategie; Für jede Position werden automatisch schützende Stop-Loss-/Take-Profit-Level registriert.
