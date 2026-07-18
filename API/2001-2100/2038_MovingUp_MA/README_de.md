# MovingUp MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein gleitender-Durchschnitt-Kreuzungssystem mit optionalem Risikomanagement.
Sie eröffnet eine Long-Position, wenn der schnelle gleitende Durchschnitt den langsamen von unten nach oben kreuzt, und eine Short-Position beim umgekehrten Kreuzung.

## Parameter
- **Fast MA** (`FastLength`): Periode des schnellen einfachen gleitenden Durchschnitts.
- **Slow MA** (`SlowLength`): Periode des langsamen einfachen gleitenden Durchschnitts.
- **Use TP** (`UseTakeProfit`): Aktiviert die Take-Profit-Regel.
- **TP** (`TakeProfit`): Distanz im Preis für die Gewinnmitnahme.
- **Use SL** (`UseStopLoss`): Aktiviert die Stop-Loss-Regel.
- **SL** (`StopLoss`): Distanz im Preis für den Stop-Loss.
- **Use TS** (`UseTrailingStop`): Aktiviert die Trailing-Stop-Logik.
- **TS** (`TrailingStop`): Trailing-Stop-Distanz im Preis.
- **Candle** (`CandleType`): Kerzentyp für Berechnungen.

## Handelslogik
1. Kerzendaten abonnieren und zwei SMA-Indikatoren berechnen.
2. Kreuzungen von schnellem und langsamem MA erkennen.
3. Long eingehen, wenn der schnelle MA den langsamen von unten nach oben kreuzt, falls keine Long-Position besteht.
4. Short eingehen, wenn der schnelle MA den langsamen von oben nach unten kreuzt, falls keine Short-Position besteht.
5. Risikomanagement bei jeder neuen Kerze anwenden:
   - Take-Profit, wenn der Kurs die angegebene Distanz voranlegt.
   - Stop-Loss, wenn sich der Kurs um die angegebene Distanz gegen die Position bewegt.
   - Trailing-Stop sichert den Gewinn, sobald sich der Kurs günstig entwickelt.

## Ursprüngliche MQL-Strategie
Das ursprüngliche MQL4-Skript `ma_v_1_3_3.mq4` enthält zahlreiche zusätzliche Funktionen wie Schritt-hoch/runter-Logik und komplexe Positionsgrößen. Diese C#-Version konzentriert sich auf die zentrale gleitende-Durchschnitt-Kreuzung und wesentliche Risikokontrollen.
