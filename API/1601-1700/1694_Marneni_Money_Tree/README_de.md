# Marneni Money Tree Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie überträgt den MQL-Expertenberater "Marneni Money Tree" nach StockSharp.
Sie stützt sich auf einen einfachen gleitenden Durchschnitt (SMA) mit 40 Perioden und zwei verschobene Werte, um die Trendrichtung zu erkennen.
Wenn der um vier Bars verschobene SMA zwischen dem aktuellen SMA und dem Wert vor dreißig Bars liegt,
- wird eine Marktorder in der erkannten Richtung gesendet;
- werden acht zusätzliche Limitorders in zunehmenden Abständen platziert, definiert durch `Order2Pips` bis `Order9Pips`.

Long-Setups platzieren Buy-Limits unterhalb des aktuellen Preises. Short-Setups platzieren Sell-Limits oberhalb des Preises.
Positionen werden geschlossen und verbleibende Orders storniert, wenn sich die SMA-Beziehung umkehrt.

## Parameter
- `Order2Pips`–`Order9Pips` — Abstand in Pips für Limitorders 2 bis 9.
- `CandleType` — Zeitrahmen für Berechnungen.

Das Basishandelsvolumen ist auf 2 festgelegt und kann durch Ändern der Eigenschaft `Volume` vor dem Start der Strategie angepasst werden.
