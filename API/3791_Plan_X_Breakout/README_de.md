# Plan-X-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Plan-X-Breakout-Strategie repliziert den MetaTrader-Expertenberater „Plan Es konzentriert sich auf die Londoner Vormittagssitzung und wartet vor dem Einstieg darauf, dass der Preis von einer Referenzkerze abweicht. Es kann jeweils nur eine Nettoposition offen sein, und das Risiko wird durch Pip-basierte Stopps kontrolliert, die hinter dem Trade zurückbleiben, wenn er sich zu seinen Gunsten entwickelt.

## Handelslogik

1. **Sitzungsanker**
   - Die Strategie berücksichtigt 15-Minuten-Kerzen.
   - Zur konfigurierten Startzeit der Sitzung (Standard 11:00) wird das Schließen dieser Kerze aufgezeichnet. Dieser Schlusskurs dient als Ankerpreis für den Rest der Sitzung.
   - Der Handel wird erst in Betracht gezogen, nachdem mindestens eine weitere Kerze geschlossen wurde und vor der Sitzungsendestunde (Standard 15:00 Uhr).

2. **Eintrittsbedingungen**
   - **Long**: Wenn die zuletzt abgeschlossene Kerze mehr als `LongTargetPips` (Standard 25 Pips) über dem Ankerschluss schließt und keine Position offen ist.
   - **Short**: Wenn die zuletzt abgeschlossene Kerze mehr als `ShortTargetPips` (Standard 20 Pips) unter dem Ankerschluss schließt und keine Position offen ist.
   - Alle Vergleiche werden in Pip-Einheiten durchgeführt, die von der Tick-Größe des Instruments abgeleitet werden.

3. **Positionsmanagement**
   - Ein fester anfänglicher Stop-Loss in Höhe von `InitialStopPips` (Standard 25 Pips) wird relativ zum Einstiegspreis festgelegt.
   - Der Stop wird in einen Trailing Stop umgewandelt, sobald der Trade mindestens `TrailTriggerPips` (Standard 10 Pips) gewinnt.
   - Jedes Mal, wenn der Preis um weitere `TrailTriggerPips` steigt, wird der Stop um `TrailStepPips` (Standard 5 Pips) weiter in die profitable Richtung verschoben.
   - Wenn der Preis den Stop erreicht, wird die Position zum Marktwert geschlossen.

4. **Volumen**
   - Bestellungen verwenden den Parameter `TradeVolume` (Standard 0,1 Lots). Passen Sie es an die Größe des Sicherheitsvertrags an.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `TradeVolume` | Marktauftragsvolumen, das für Ein- und Ausstiege verwendet wird. | 0,1 |
| `LongTargetPips` | Für lange Einstiege ist eine Ausbruchsdistanz über dem Anker erforderlich. | 25 |
| `ShortTargetPips` | Für kurze Einstiege ist eine Ausbruchsdistanz unterhalb des Ankers erforderlich. | 20 |
| `InitialStopPips` | Abstand vom Einstiegspreis bis zum schützenden Stop-Loss. | 25 |
| `TrailTriggerPips` | Gewinn in Pips, der benötigt wird, bevor der Trailing Stop aktiviert wird oder vorrückt. | 10 |
| `TrailStepPips` | Pip-Inkrement, das bei jeder Bewegung auf den Trailing Stop angewendet wird. | 5 |
| `SessionStartHour` | Dezimale Stunde, die angibt, wann die Ankerkerze beginnt (z. B. `11.0`, `11.5`). | 11.0 |
| `SessionEndHour` | Dezimale Stunde, nach der keine neuen Einträge mehr vorgenommen werden. Muss später als `SessionStartHour` sein. | 15.0 |
| `CandleType` | Für Auswertungen verwendete Kerzenserien. Standardmäßig sind 15-Minuten-Kerzen eingestellt. | 15 Minuten |

## Notizen

- Die Pip-Größe passt sich automatisch an, basierend auf dem `PriceStep` des Instruments und der Dezimalgenauigkeit (3 oder 5 Dezimalstellen erhalten einen 10-fachen Multiplikator).
- Die Strategie geht von einem kontinuierlichen Intraday-Markt aus; Bei Instrumenten mit täglichen Lücken kommt es an jedem Handelstag zu einem Re-Anker-Verhalten.
- Da StockSharp-Strategien Nettopositionen verwenden, geht die Konvertierung jeweils nur von einer offenen Richtung aus. Dies spiegelt das Standardverhalten des ursprünglichen Experten wider, wenn keine Absicherung aktiv ist.

## Dateien

- `CS/PlanXBreakoutStrategy.cs` – C#-Implementierung der Plan X-Breakout-Logik für StockSharp.
