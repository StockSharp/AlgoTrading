# X Trader V2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein konträres gleitender-Durchschnitt-Kreuzungssystem, das vom ursprünglichen MQL4-Experten **X_trader_v2** konvertiert wurde. Es verwendet zwei gleitende Durchschnitte, um plötzliche Umkehrungen zu erkennen, und führt Trades entgegen der Kreuzungsrichtung aus.

## Funktionsweise
1. Zwei einfache gleitende Durchschnitte werden auf dem ausgewählten Zeitrahmen berechnet.
2. Wenn der schnelle MA den langsamen MA **nach oben** kreuzt, eröffnet die Strategie eine **Short**-Position.
3. Wenn der schnelle MA den langsamen MA **nach unten** kreuzt, eröffnet die Strategie eine **Long**-Position.
4. Es kann immer nur eine Position gleichzeitig offen sein. Ein neuer Trade wird erst platziert, nachdem der vorherige geschlossen wurde und ein neues Signal erscheint.
5. Der integrierte Schutz platziert automatisch Stop-Loss- und Take-Profit-Orders.

## Parameter
- `Ma1Period` – Periode des schnellen gleitenden Durchschnitts.
- `Ma2Period` – Periode des langsamen gleitenden Durchschnitts.
- `TakeProfitTicks` – Take-Profit-Abstand in Preis-Ticks.
- `StopLossTicks` – Stop-Loss-Abstand in Preis-Ticks.
- `CandleType` – Kerzentyp für Berechnungen.

## Hinweise
- Die Strategie abonniert Kerzendaten über die High-Level-API.
- Indikatorwerte werden über Bindings verarbeitet, ohne direkte Aufrufe von `GetValue`.
- Der Algorithmus speichert vorherige Indikatorwerte intern, um aufwändige Historienabfragen zu vermeiden.
