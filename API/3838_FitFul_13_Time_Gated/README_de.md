# FitFul 13 Time Gated-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **FitFul 13 Time Gated Strategy** ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „FitFul_13“. Die Strategie erstellt eine wöchentliche Pivot-Leiter (PP, R0,5, R1, R1,5, R2, R2,5, R3 und die entsprechenden Unterstützungsniveaus) unter Verwendung der Höchst-, Tiefst- und Schlusskurse der Vorwoche. Handelsentscheidungen werden im primären Zeitrahmen (Standard 1 Stunde) getroffen und optional durch einen schnelleren Zeitrahmen (Standard 15 Minuten) bestätigt. Neue Positionen sind nur zu bestimmten Intraday-Minuten zulässig, um das ursprüngliche EA-Verhalten nachzuahmen.

## Signallogik
1. **Wöchentliche Pivot-Berechnung**
   - Am Ende jeder wöchentlichen Kerze wird die Pivot-Leiter neu berechnet.
   - Stop-Loss- und Take-Profit-Preise werden von den Basisniveaus um einen konfigurierbaren Abstand, ausgedrückt in Preispunkten, versetzt.
2. **Primäre Zeitrahmenbedingungen**
   - Die letzte abgeschlossene Primärkerze muss bullisch sein, um nach Long-Einstiegen zu suchen, oder bärisch, um nach Short-Einstiegen zu suchen.
   - Die vorherige Primärkerze muss eine der Pivot-Ebenen überspannen (unten öffnen und oben schließen für Long-Positionen, oben öffnen und unten schließen für Short-Positionen).
3. **Bedingungen für den Bestätigungszeitraum**
   - Wenn die aktuelle Bestätigungskerze bullisch ist, müssen die Tiefs der beiden vorherigen Bestätigungskerzen das gleiche Pivot-Level durchbrechen und darüber schließen, um ein Long-Signal zu bestätigen.
   - Wenn die aktuelle Bestätigungskerze bärisch ist, müssen die Höchststände der beiden vorherigen Bestätigungskerzen ein Pivot-Level durchbrechen und darunter schließen, um ein Short-Signal zu bestätigen.
4. **Eintrittszeitpunkt**
   - Ein Handel wird nur dann platziert, wenn die Eröffnungsminute der fertigen Primärkerze einer der vier konfigurierten Minuten entspricht (standardmäßig 0, 15, 30 oder 45).
   - Das Nettorisiko ist auf `MaxNetPositions × Volume` begrenzt, um die Beschränkung „maximal drei offene Bestellungen“ der Version MetaTrader zu emulieren.

## Risikomanagement
- **Stopps und Ziele** – Jeder Position wird unmittelbar nach dem Einstieg ein vom Pivot abgeleiteter Stop-Loss und Take-Profit zugewiesen.
- **Trailing Stop** – Sobald der Preis um die konfigurierte Anzahl von Punkten steigt, wird der Stop in Handelsrichtung nachgezogen.
- **Maximale Haltezeit** – Profitable Geschäfte werden geschlossen, sobald die Haltezeit die konfigurierte Dauer überschreitet (standardmäßig 48 Stunden).
- **Freitags-Flat-Regel** – Freitags wird jede offene Position zwischen den konfigurierten Minuten der angegebenen Stunde (Standard 21:50–21:59) geschlossen.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `PrimaryCandleType` | Zeitrahmen, der für die Haupt-Pivot-Gegenprüfungen verwendet wird. |
| `ConfirmationCandleType` | Schnellerer Zeitrahmen, der Pivot-Reaktionen validiert. |
| `Volume` | Netto-Marktauftragsvolumen. |
| `MaxNetPositions` | Maximale Belichtung, gemessen in Vielfachen von `Volume`. |
| `OffsetPoints` | Preispunktabstand, der auf Stopps und Ziele um jeden Drehpunkt angewendet wird. |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Preispunkten. |
| `CloseAfter` | Maximale Haltezeit für profitable Positionen. |
| `CloseHour`, `CloseMinuteFrom`, `CloseMinuteTo` | Freitags-Zeitfenster für erzwungene Ausgänge. |
| `EntryMinute0..3` | Erlaubte Minuten (innerhalb jeder Stunde) für die Eröffnung neuer Positionen. |

## Notizen
- Die Konvertierung behält die ursprüngliche Abhängigkeit von EA von der Pivot-Leiter und den viertelstündigen Ausführungsfenstern der Vorwoche bei.
- Die Geldverwaltung wurde vereinfacht: Der Parameter StockSharp `Volume` steuert die Auftragsgröße direkt, anstatt die dynamische Losberechnung von MetaTrader erneut zu implementieren.
- Alle Kommentare im Code sind gemäß den Projektrichtlinien auf Englisch verfasst.
