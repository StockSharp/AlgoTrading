# JS Chaos-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die JS Chaos-Strategie repliziert das Verhalten des ursprünglichen MetaTrader Expert Advisors "JS-Chaos" mit der StockSharp High-Level-API. Die Strategie baut Ausbruchseinstiege um Bill Williams' Alligator-Struktur und Fraktallevels auf, kombiniert Awesome Oscillator- und Acceleration/Deceleration-Bestätigung und verwaltet offene Exposition mit Trailing Stops, Breakeven-Logik und einem umfangreichen Zeitfilter.

## Kernlogik
1. **Indikatorstapel**
   - Bill Williams Alligator (Geglättete Moving Averages mit 13/8/5 Perioden und 8/5/3 Balkenverschiebungen) auf dem Medianpreis abgetastet.
   - Awesome Oscillator und ein 5-Perioden-SMA von AO zur Ableitung des Acceleration/Deceleration-Oszillators.
   - 21-Perioden-geglätteter Moving Average für den Trailing-Stop-Motor.
   - 10-Perioden-Standardabweichung als zusätzliche Trailing-Bedingung verwendet.
   - Fraktalerkennung über die letzten fünf Hochs/Tiefs mit Speicherung der jüngsten Formationen für zehn Balken.
2. **Signalgenerierung**
   - Bullischer Kontext erfordert `AO[0] > AO[1] > 0` und `Lips > Teeth > Jaw`.
   - Bärischer Kontext erfordert `AO[0] < AO[1] < 0` und `Lips < Teeth < Jaw`.
3. **Auftragsplatzierung**
   - Wenn Bedingungen übereinstimmen und die aktuelle Zeit handelbar ist, stellt die Strategie zwei Stop-ähnliche Einstiege pro Richtung in die Warteschlange: einen primären Auftrag (2× Basisvolumen) und einen sekundären Auftrag (1× Basisvolumen). Beide lösen am jüngsten qualifizierenden Fraktal aus, das über die Alligator-Lippen hinausgeht.
   - Primäres Take-Profit verwendet `Lips ± (Fractal − Lips) * Fibo1`. Sekundäres Take-Profit verwendet den `Fibo2`-Multiplikator.
4. **Handelsmanagement**
   - Optionaler frühzeitiger Ausstieg, wenn die Lippen über (für Longs) oder unter (für Shorts) die Eröffnung der vorherigen Kerze kreuzen.
   - Trailing Stop zieht das Schutzlevel zur 21-Perioden-SMMA, wenn Standardabweichung, AO und AC alle in der Handelsrichtung vorankommen.
   - Breakeven-Logik verschiebt den sekundären Trade-Stop, sobald der primäre Trade abgeschlossen ist und der Preis um die konfigurierten Extra-Pips fortgeschritten ist.
   - Manuelle Überwachung von Stop-Loss- und Take-Profit-Levels schließt Trades über Marktaufträge, wenn die entsprechenden Preisgrenzen überschritten werden.
5. **Zeitfilter**
   - Handelsfenster definiert durch Start-/Endstunden (mit Wrap-around-Unterstützung) und optionalen saisonalen Filtern: deaktiviert vor Montag 03:00, nach Freitag 18:00, während der ersten neun Tage im Januar und nach dem 20. Dezember. Setzen von `Use Time` auf false deaktiviert den Filter vollständig.

## Parameter
| Name | Beschreibung |
| ---- | ----------- |
| `UseTime` | Aktiviert den Zeitfilter. |
| `OpenHour` / `CloseHour` | Stundengrenzen für den Handel (0-23). |
| `BaseVolume` | Basisauftragsvolumen, verwendet zur Dimensionierung der zwei gestaffelten Einstiege (2× für den primären, 1× für den sekundären). |
| `IndentingPips` | Offset der zu Fraktallevels vor der Platzierung von Stop-Aufträgen hinzugefügt/abgezogen wird (in Pips). |
| `Fibo1` / `Fibo2` | Fibonacci-ähnliche Multiplikatoren, angewendet auf den Abstand zwischen den Lippen und dem Fraktal für die Take-Profit-Ziele. |
| `UseClosePositions` | Schließt entgegengesetzte Positionen, wenn die Lippen die vorherige Kerzeneröffnung kreuzen. |
| `UseTrailing` | Aktiviert den MA/Oszillator-basierten Trailing Stop. |
| `UseBreakeven` | Aktiviert das Breakeven-Management für die sekundäre Position. |
| `BreakevenPlusPips` | Extra Pips, die zum Einstiegspreis hinzugefügt werden, wenn der Stop auf Breakeven verschoben wird. |
| `CandleType` | Zeitrahmen der von der Strategie verarbeiteten Kerzen. |

## Hinweise
- Die Konvertierung behält die gestaffelte Auftragsstruktur und Verwaltungslogik des ursprünglichen MQL5-Roboters bei und nutzt gleichzeitig den Kerzen-Subskriptions-Workflow von StockSharp.
- Alle Berechnungen basieren auf fertigen Kerzen; die Intrabar-Tick-Logik des ursprünglichen EAs wird durch Marktaufträge gespiegelt, sobald der Preisbereich einen Ausbruch bestätigt.
- Die Pip-Konvertierung passt sich automatisch an Instrumente an, die mit drei oder fünf Dezimalstellen notieren (forex-ähnliche Symbole).
