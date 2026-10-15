# Line-Order-Einzeleinstieg-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Line-Order-Strategie ist eine Übersetzung des MQL4-Skripts "LineOrder" (10715). Die Strategie eröffnet eine Position, wenn der Marktpreis eine vordefinierte Einsteigslinie erreicht, und verwaltet dann die Position mit Stop-Loss, Take-Profit und einem optionalen Trailing Stop.

## Parameter

- `Entry Price` – Preisniveau, das eine Position auslöst.
- `Stop Loss (pips)` – Abstand vom Einstieg zum anfänglichen Stop-Loss.
- `Take Profit (pips)` – Abstand vom Einstieg zum Take-Profit.
- `Trailing Stop (pips)` – optionaler Trailing-Stop-Abstand. Bei Null wird das Trailing deaktiviert.
- `Candle Type` – Art der für die Verarbeitung verwendeten Kerzen.

## Handelslogik

1. Die Strategie abonniert die ausgewählte Kerzenserie.
2. Wenn eine abgeschlossene Kerze über dem Einstiegspreis schließt, wird eine Long-Position eröffnet. Wenn sie darunter schließt, wird eine Short-Position eröffnet.
3. Nach dem Einstieg werden Stop-Loss- und Take-Profit-Niveaus anhand des Preisschritts des Instruments berechnet.
4. Wenn der Trailing Stop aktiviert ist, bewegt sich das Stop-Niveau in Richtung des Trades.
5. Die Position wird geschlossen, wenn der Preis entweder den Stop-Loss- oder den Take-Profit-Level erreicht.

Dies ist eine vereinfachte Portierung des ursprünglichen MQL-Skripts, die sich auf die automatisierte Orderausführung an einer benutzerdefinierten Linie konzentriert.
