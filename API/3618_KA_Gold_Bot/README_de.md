# KA-Gold Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **KA-Gold-Bot-Strategie** ist eine hochrangige StockSharp-Konvertierung des ursprünglichen MetaTrader 4 „KA-Gold Bot“-Expertenberaters. Es kombiniert einen Kanal im Keltner-Stil mit Trendfiltern und einem aggressiven Risikomanagement, das feste Stop-Loss-, Take-Profit- und mehrstufige Trailing-Schutzfunktionen umfasst. Der Handel ist nur während eines konfigurierbaren Intraday-Fensters erlaubt und neue Positionen werden blockiert, wenn der Live-Spread einen Schwellenwert überschreitet.

## Handelslogik

1. **Indikatorvorbereitung**
   - Ein exponentieller gleitender Durchschnitt (EMA) mit der Länge `KeltnerPeriod` bildet die Kanalmittellinie.
   - Ein einfacher gleitender Durchschnitt der Kerzenbereiche (Hoch minus Tief) mit derselben Periode schätzt die Halbwertsbreite des Kanals.
   - Kurzfristige und langfristige exponentielle gleitende Durchschnitte (`EmaShortPeriod` und `EmaLongPeriod`) verfolgen die schnelle Dynamik bzw. den Trend im höheren Zeitrahmen.
   - Alle Indikatorwerte werden für die beiden zuletzt abgeschlossenen Kerzen aufgezeichnet, um die MT4-schichtbasierten Berechnungen widerzuspiegeln.

2. **Eintrittsbedingungen**
   - Berechnungen werden nur ausgeführt, wenn die aktuelle Kerze schließt und die Strategie mit erteilten Handelserlaubnissen mit dem Markt verbunden ist.
   - Die oberen und unteren Bänder des Kanals werden durch Addition/Subtraktion des durchschnittlichen Bereichs von der EMA-Mittellinie sowohl für die vorherige (`shift = 1`) als auch für die frühere (`shift = 2`) Kerze abgeleitet.
   - **Lange Einrichtung:**
     - Der vorherige Schlusskurs durchbricht das jüngste obere Band.
     - Der gleiche Schlusskurs liegt über dem Long-Kurs EMA, was einen Aufwärtstrend bestätigt.
     - Das kurze EMA kreuzt von unterhalb des älteren oberen Bandes nach oberhalb des neuesten (`EMA_short[2] < Upper[2]` und `EMA_short[1] > Upper[1]`).
   - **Kurzer Aufbau:**
     - Der vorherige Schlusskurs liegt unter dem jüngsten unteren Band.
     - Der gleiche Schlusskurs liegt unter dem Long-Kurs EMA, was einen Abwärtstrend bestätigt.
     - Das kurze EMA kreuzt von oberhalb des älteren unteren Bandes bis unterhalb des neuesten (`EMA_short[2] > Lower[2]` und `EMA_short[1] < Lower[1]`).
   - Es ist jeweils nur eine Position zulässig. Wenn ein Trade bereits offen ist, wird das Signal ignoriert.

3. **Timing- und Spread-Filter**
   - Wenn `UseTimeFilter` aktiviert ist, sind neue Einträge auf das Fenster `[StartHour:StartMinute, EndHour:EndMinute)` unter Verwendung der Börsen-Ortszeit beschränkt. Nachtsitzungen werden unterstützt, wenn die Endzeit vor der Startzeit liegt.
   - Angebotsabonnements der Stufe 1 verfolgen die besten Geld-/Briefkurse. Vor der Auftragserteilung rechnet die Strategie den aktuellen Spread in Instrumentenpunkte um und vergleicht ihn mit `MaxSpreadPoints`. Bestellungen werden mit Protokollierung übersprungen, wenn der Schwellenwert überschritten wird.

4. **Risikomanagement**
   - Die Positionsgröße ist standardmäßig auf `FixedVolume` eingestellt. Wenn `UseRiskPercent` den Wert `true` hat, wird die Handelsgröße aus dem Portfolioeigenkapital als `RiskPercent% / (riskPips * PipValue)` neu berechnet, wobei `riskPips` gleich `StopLossPips` ist (Fallback auf `TrailingStopPips`, wenn kein fester Stop definiert ist). Das Endergebnis wird auf den Instrumentenvolumenschritt normiert und zwischen den minimalen und maximalen Austauschgrenzen eingeklemmt.
   - Wenn eine Long-Position eröffnet wird, speichert die Strategie Folgendes:
     - Anfänglicher Stop-Loss bei `entry - StopLossPips * pipSize` (falls definiert).
     - Anfänglicher Take-Profit bei `entry + TakeProfitPips * pipSize` (falls definiert).
     - Nachfolgende Statusflags, die die Short-Side-Tracker zurücksetzen.
   - Short-Trades spiegeln die gleiche Logik mit umgekehrten Preisrichtungen wider.

5. **Nachlaufschutz**
   - Live-Bid/Ask-Updates versorgen zwei nachfolgende Engines:
     - Sobald der variable Gewinn `TrailingTriggerPips` überschreitet, wird das Trailing aktiv.
     - Der Trailing-Stop ist `TrailingStopPips` vom aktuell günstigen Preis entfernt positioniert und wird nur dann vorgezogen, wenn die Bewegung um mehr als `TrailingStopPips + TrailingStepPips` über das vorherige Stop-Level hinausgeht.
     - Bei Long-Positionen fällt der Trailing Stop nie unter den ursprünglichen Schutzstopp und bei Short-Positionen steigt er nie darüber.
   - Die Exit-Überwachung erfolgt sowohl für eingehende Quotes als auch für abgeschlossene Kerzen:
     - Eine Position wird sofort geschlossen, wenn der Preis den aktiven Stop (Original oder Trailing) erreicht.
     - Gewinne werden auch gesperrt, sobald das Hoch/Tief der Kerze das gespeicherte Take-Profit-Niveau berührt.
   - Nach dem Schließen einer Position wird der Schutzstatus vollständig zurückgesetzt, um veraltete Daten zu vermeiden.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Datentyp, der den Ausführungszeitrahmen beschreibt. | Zeitrahmen von 1 Minute |
| `KeltnerPeriod` | Zeitraum für die EMA-Mittellinie und den Bereichsdurchschnitt des Kanals. | 50 |
| `EmaShortPeriod` | Schnelle EMA-Länge, die zur Crossover-Bestätigung verwendet wird. | 10 |
| `EmaLongPeriod` | Langsame EMA-Länge, die als Trendfilter fungiert. | 200 |
| `FixedVolume` | Fallback-Bestellvolumen, wenn die prozentuale Größenanpassung deaktiviert ist. | 1 |
| `UseRiskPercent` | Aktivieren Sie die prozentuale Positionsgrößenbestimmung. | `true` |
| `RiskPercent` | Prozentsatz des pro Trade riskierten Eigenkapitals. | 1 |
| `StopLossPips` | Abstand des festen Stop-Loss in Pips (0 deaktiviert). | 500 |
| `TakeProfitPips` | Abstand des festen Take-Profits in Pips (0 deaktiviert). | 500 |
| `TrailingTriggerPips` | Gewinn in Pips, der zur Aktivierung des Trailing Stop erforderlich ist. | 300 |
| `TrailingStopPips` | Abstand zwischen Preis und Trailing Stop, sobald er aktiv ist. | 300 |
| `TrailingStepPips` | Minimaler zusätzlicher Gewinn (in Pips), bevor der Trailing Stop erhöht wird. | 100 |
| `UseTimeFilter` | Schalten Sie den Handelssitzungsfilter um. | `true` |
| `StartHour` / `StartMinute` | Sitzungsbeginn in Börsen-Ortszeit. | 02:30 |
| `EndHour` / `EndMinute` | Die Sitzung endet in Börsen-Ortszeit. | 21:00 |
| `MaxSpreadPoints` | Maximal zulässige Streuung in Instrumentenpunkten (0 deaktiviert die Prüfung). | 65 |
| `PipValue` | Geldwert eines Pip, der zur risikobasierten Positionsgrößenbestimmung verwendet wird. | 1 |

## Zusätzliche Hinweise

- Die Pip-Umrechnung folgt den Dezimalzahlen des Börseninstruments: Ein fünfstelliger Kurs (ungerade Anzahl von Dezimalstellen) multipliziert den Preisschritt mit 10, um die MT4-Pip-Größenlogik zu emulieren.
- Die Strategie abonniert sowohl Kerzen- als auch Level-1-Daten, registriert aber **keine** zusätzlichen Indikatoren im Diagramm und entspricht damit den High-Level-API-Richtlinien.
- Schutzausstiege beruhen auf Marktaufträgen, die von der Strategie erteilt werden. Es werden keine separaten Stop- oder Limit-Orders an der Börse platziert.
- Python-Unterstützung ist in dieser Lieferung nicht enthalten, entsprechend der ursprünglichen Anfrage.
