# Alligator-Trendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie reproduziert das klassische Bill Williams Alligator-System aus dem originalen MetaTrader-Skript (`Alligator.mq5`). Sie verwendet drei geglättete gleitende Durchschnitte, die auf dem Median-Preis berechnet und vorwärts verschoben werden, um die Marktphase zu visualisieren. Eine Long-Position wird eröffnet, wenn die schnelle Lips-Linie über Teeth liegt und Teeth über Jaw. Eine Short-Position wird eröffnet, wenn die Ausrichtung umgekehrt ist. Nur eine Position kann gleichzeitig aktiv sein.

Sobald ein Trade aktiv ist, schützt die Strategie die Position mit einem Stop-Loss und Take-Profit in Pips. Wenn sich der Markt um eine konfigurierbare Null-Level-Distanz zugunsten des Trades bewegt, wird der Stop auf Break-even verschoben. Ein Trailing Stop folgt dem höchsten Hoch (bei Longs) oder tiefsten Tief (bei Shorts) mit einem Mindestschritt, um häufige Stop-Aktualisierungen zu vermeiden. Positionen werden geschlossen, wenn die Stop-Loss-, Trailing-Stop- oder Take-Profit-Level erreicht werden.

Die Standardkonfiguration zielt auf 30-Minuten-Kerzen und Forex-ähnliche Pip-Werte ab, aber die Parameter können für andere Märkte optimiert werden. Da die ursprüngliche MQL-Version die brokerspezifische Pip-Verarbeitung verwendet, basiert die Konvertierung auf dem `PriceStep` des Instruments, um Pip-Abstände in absolute Preise umzurechnen.

## Handelsregeln

### Einstieg
- **Long**: Keine offene Position und `Lips > Teeth > Jaw` auf der letzten abgeschlossenen Kerze.
- **Short**: Keine offene Position und `Lips < Teeth < Jaw` auf der letzten abgeschlossenen Kerze.

### Ausstieg und Risikomanagement
- **Anfangs-Stop**: Platziert `StopLossPips` unterhalb (Long) oder oberhalb (Short) des Ausführungspreises.
- **Take Profit**: Platziert `TakeProfitPips` vom Ausführungspreis entfernt.
- **Null-Level**: Wenn der Preis um `ZeroLevelPips` vorrückt, wird der Stop auf den Einstiegspreis verschoben.
- **Trailing Stop**: Nach der Null-Level-Aktivierung folgt der Stop dem Extremwert mit `TrailingStopPips` und wird nur aktualisiert, wenn die Verbesserung `TrailingStepPips` überschreitet.
- Positionen werden sofort aufgelöst, wenn ein Stop oder das Take-Profit-Level auf Kerzendaten berührt wird.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `CandleType` | 30-Minuten-Zeitrahmen | Kerzenserie für Indikatorberechnungen und Signalauswertung. |
| `JawLength` | 13 | Geglättete Moving-Average-Periode für die blaue Jaw-Linie. |
| `TeethLength` | 8 | Geglättete Moving-Average-Periode für die rote Teeth-Linie. |
| `LipsLength` | 5 | Geglättete Moving-Average-Periode für die grüne Lips-Linie. |
| `JawShift` | 8 | Vorwärtsverschiebung der Jaw-Linie, in Bars angegeben. |
| `TeethShift` | 5 | Vorwärtsverschiebung der Teeth-Linie, in Bars angegeben. |
| `LipsShift` | 3 | Vorwärtsverschiebung der Lips-Linie, in Bars angegeben. |
| `EnableLong` | `true` | Erlaubt oder sperrt Long-Einstiege. |
| `EnableShort` | `true` | Erlaubt oder sperrt Short-Einstiege. |
| `StopLossPips` | 45 | Stop-Loss-Abstand in Pips vom Ausführungspreis. |
| `TakeProfitPips` | 145 | Take-Profit-Abstand in Pips vom Ausführungspreis. |
| `ZeroLevelPips` | 30 | Abstand in Pips, der erforderlich ist, um den Stop auf Break-even zu verschieben. |
| `TrailingStopPips` | 50 | Abstand zwischen dem aktuellen Extremwert und dem Trailing Stop. |
| `TrailingStepPips` | 10 | Minimale Pip-Verbesserung, die vor der Aktualisierung des Trailing Stops erforderlich ist. |

## Hinweise

- Der Alligator-Indikator wird auf dem Median-Preis `(High + Low) / 2` berechnet, um der MetaTrader-Implementierung zu entsprechen.
- Verschobene Linienwerte werden mit internen Puffern emuliert, so dass Vergleiche dieselben verschobenen Daten wie das Originalskript verwenden.
- Die Strategie geht davon aus, dass ein Trade ausgeführt wird, bevor ein neues Signal auf derselben Bar verarbeitet wird, entsprechend der Bar-für-Bar-Ausführung des Quell-EAs.
- Optimieren Sie die Pip-Abstände entsprechend der Tick-Größe und Volatilität des gehandelten Instruments.
