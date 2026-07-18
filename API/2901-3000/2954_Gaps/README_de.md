# Gaps-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine Price-Action-Strategie, die auf Eröffnungslücken zwischen aufeinanderfolgenden Kerzen reagiert. Sie wartet darauf, dass eine neue Bar
 über das vorherige Hoch oder Tief um eine konfigurierbare Pip-Distanz hinausöffnet, tritt in die Richtung der erwarteten Umkehr ein und
 verwaltet den Trade mit festen Stops, Zielen und einem optionalen gestuften Trailing-Stop.

## Funktionsweise

1. Die Strategie überwacht ein einzelnes Symbol mit dem gewählten Zeitrahmen.
2. Wenn eine neue Kerze gebildet wird, vergleicht sie den Eröffnungspreis mit der vorherigen Kerze:
   - Wenn die Eröffnung unter dem vorherigen Tief minus `GapPips` liegt, tritt die Strategie in eine Long-Position ein und erwartet einen bullischen Rücksetzer.
   - Wenn die Eröffnung über dem vorherigen Hoch plus `GapPips` liegt, tritt sie in eine Short-Position ein und antizipiert eine Abwärtskorrektur.
3. Im Trade wird das Risikomanagement vollständig innerhalb der Strategie verwaltet:
   - Ein fester Stop-Loss wird `StopLossPips` unterhalb (für Long) oder oberhalb (für Short) des Einstiegspreises gesetzt.
   - Ein fester Take-Profit wird `TakeProfitPips` vom Einstieg in Richtung des Trades gesetzt.
   - Ein Trailing-Stop kann aktiviert werden; er bewegt sich erst, nachdem der Preis um `TrailingStopPips + TrailingStepPips` vorgezogen hat, und sichert
     dann Gewinne, indem er den Stop `TrailingStopPips` vom günstigsten Preis entfernt hält.
4. Schutzlevels werden bei jeder abgeschlossenen Kerze anhand der Hoch/Tief-Extremwerte ausgewertet, sodass Intrabar-Berührungen zuverlässig Ausstiege auslösen.
5. Offene Orders werden vor einer neuen Position storniert, und Positionsumkehrungen schließen automatisch die entgegengesetzte Seite.

## Parameter

- `OrderVolume` = 0.1 — Handelsvolumen in Lots für jede neue Position.
- `StopLossPips` = 50 — Abstand vom Einstiegspreis zum Stop-Loss-Level in Pips. Auf 0 setzen zum Deaktivieren.
- `TakeProfitPips` = 50 — Abstand vom Einstiegspreis zum Take-Profit-Level in Pips. Auf 0 setzen zum Deaktivieren.
- `TrailingStopPips` = 5 — Größe des Trailing-Stops in Pips. Auf 0 setzen zum Deaktivieren.
- `TrailingStepPips` = 5 — Minimale Preisverbesserung (in Pips), bevor sich der Trailing-Stop wieder bewegt.
- `GapPips` = 1 — Minimale Eröffnungslücke, in Pips ausgedrückt, die zum Erzeugen eines Einstiegssignals erforderlich ist.
- `CandleType` = 1-Stunden-Zeitrahmen — Kerzen, die für die Lückenerkennung und Positionsmanagement verwendet werden.

## Implementierungshinweise

- Pip-basierte Eingaben werden mit der Instrument-Tick-Größe in absolute Preisabstände umgerechnet. Fünfstellige und dreistellige Forex-
  Kurse werden automatisch angepasst, um mit echten Pip-Werten zu arbeiten.
- Die Trailing-Stop-Logik erfordert, dass `TrailingStepPips` positiv ist, wenn `TrailingStopPips` aktiviert ist; andernfalls wirft die Strategie
  beim Start eine Ausnahme, was die ursprüngliche MQL-Validierung widerspiegelt.
- Die Strategie bewertet Risikokontrollen nur bei fertigen Kerzen gemäß den Richtlinien der StockSharp High-Level-API.
- Manuelle Stop- und Ziel-Verwaltung basiert auf Marktorders, es gibt also keine separaten Schutzorders im Orderbuch.
- Standardeinstellungen setzen Forex-Instrumente voraus; passen Sie die Pip-Distanzen an, wenn Sie Assets mit unterschiedlicher Volatilität oder Tick-Größen handeln.
