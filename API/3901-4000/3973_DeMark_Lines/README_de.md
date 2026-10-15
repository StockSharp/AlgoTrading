# DeMark Lines-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die DeMark Lines-Strategie ist eine Umsetzung des Indikators MetaTrader „DeMark_lines“ (MQL/8296). Das ursprüngliche Skript zeichnete DeMark-Trendlinien auf der Grundlage der jüngsten Swing-Hochs und -Tiefs und markierte Ausbrüche mit optionalen Warnungen. Diese StockSharp-Implementierung wandelt die Visualisierungslogik in eine automatisierte Breakout-Strategie um. Es sucht kontinuierlich nach Abwärts- und Aufwärtstrendlinien, die durch validierte Pivot-Punkte gebildet werden, und eröffnet Positionen, wenn die Preisbewegung diese Linien entscheidend durchbricht.

## Handelslogik
1. **Pivot-Erkennung** – fertige Kerzen werden in chronologischer Reihenfolge verarbeitet. Eine Kerze wird zu einem Swing-Hoch, wenn ihr Hoch streng höher ist als das der vorherigen *PivotDepth*-Kerzen und nicht niedriger als das der folgenden *PivotDepth*-Kerzen. Swing-Tiefs folgen der gespiegelten Bedingung für Tiefs.
2. **Trendlinienkonstruktion** – die beiden letzten Swing-Hochs bilden die aktive Abwärtstrend-Widerstandslinie. Die beiden letzten Swing-Tiefs bilden die Aufwärtstrend-Unterstützungslinie. Zusätzliche Drehpunkte werden ignoriert, wenn sie zu nahe am vorherigen Anker liegen, wodurch instabile Linien verhindert werden.
3. **Breakout-Filter** – die Strategie misst den theoretischen Trendlinienwert für den aktuellen Balkenindex. Für einen Ausbruch muss der Schlusskurs die Widerstandslinie um mindestens *BreakoutBuffer* Pips überschreiten (oder unter die Unterstützung fallen), bevor Trades ausgeführt werden.
4. **Auftragserteilung** – wenn ein zinsbullischer Ausbruch auftritt, wird jedes Short-Engagement geschlossen und eine Long-Position des konfigurierten Strategievolumens eröffnet. Die bärische Ausbruchslogik spiegelt dieses Verhalten wider. Jede Linie kann erst dann ein neues Signal auslösen, wenn ein neuer Pivot sie neu definiert, wodurch wiederholte Einträge vermieden werden, während der Preis um das Niveau herum schwebt.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `PivotDepth` | Anzahl der Kerzen auf jeder Seite, die zur Bestätigung eines Pivot-Hochs/Tiefs erforderlich sind. Steuert die Genauigkeit der Schwungerkennung. | 2 |
| `MinBarsBetweenPivots` | Mindestabstand in Balken zwischen zwei Drehpunkten desselben Typs. Verhindert überlappende Anker und hält Trendlinien stabil. | 5 |
| `BreakoutBuffer` | Zusätzlicher Abstand (in Pips), der über die Trendlinie hinaus hinzugefügt wird, bevor ein Ausbruch als gültig angesehen wird. Filtert laute Berührungen. | 2 |
| `CandleType` | Kerzendatentyp (Zeitrahmen), der für die Analyse und Signalgenerierung verwendet wird. | 30-Minuten-Kerzen |

## Konvertierungshinweise
- Visuelle Objekte, Warnungen und E-Mail-Benachrichtigungen des ursprünglichen Indikators werden nicht repliziert. Stattdessen werden in Diagrammbereichen Preisreihen und die eigenen Trades der Strategie angezeigt.
- Die Strategie basiert auf dem High-Level-Kerzenabonnement API von StockSharp und verwendet interne Puffer, um Pivots zu validieren, ohne auf Indikatorverlaufsmethoden zu verweisen, die durch die Richtlinien verboten sind.
- Breakout-Trades respektieren die Basiseigenschaft `Volume` und kehren das bestehende Risiko automatisch um, wenn der entgegengesetzte Breakout ausgelöst wird.

## Nutzungstipps
- Erhöhen Sie `PivotDepth` in höheren Zeitrahmen, um breitere Schwankungen zu erfordern, was die Signalfrequenz verringert, aber die Zuverlässigkeit der Trendlinie verbessert.
- Passen Sie `BreakoutBuffer` an, um die Volatilität des Instruments zu berücksichtigen. Enge Werte begünstigen frühere Einträge, während größere Puffer dazu beitragen, Fälschungen zu vermeiden.
- Kombinieren Sie die Strategie mit externen Money-Management- oder Schutzmodulen, wenn eine automatisierte Exit-Abwicklung (Take-Profit/Stop-Loss) erforderlich ist, da sich das ursprüngliche Skript nur auf die Breakout-Erkennung konzentrierte.
