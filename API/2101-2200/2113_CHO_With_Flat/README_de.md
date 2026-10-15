# CHO With Flat-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt basierend auf der Kreuzung des **Chaikin Oscillators** und seiner gleitenden Durchschnittslinie. Ein Bollinger-Bands-Filter wird verwendet, um Trades in Seitwärtsmärkten zu vermeiden.

## Parameter
- **Candle Type** – Zeitrahmen der Eingangskerzen.
- **Fast Period** – schnelle Periode des Chaikin Oscillators.
- **Slow Period** – langsame Periode des Chaikin Oscillators.
- **MA Period** – Periode des auf den Oszillator angewendeten gleitenden Durchschnitts.
- **MA Type** – Art des gleitenden Durchschnitts für die Signallinie.
- **Bollinger Period** – Periode der Bollinger Bands.
- **Std Deviation** – Standardabweichung für die Bollinger Bands.
- **Flat Threshold** – minimale Bandbreite (in Punkten), damit der Markt als aktiv gilt.

## Handelslogik
1. Chaikin Oscillator und seinen gleitenden Durchschnitt berechnen.
2. Bollinger Bands auf dem Preis für die Seitwärtsmarkterkennung aufbauen.
3. Trades überspringen, wenn die Bollinger-Bandbreite unter `Flat Threshold` liegt.
4. **Kaufen**, wenn der Oszillator von oben durch seine Signallinie kreuzt.
5. **Verkaufen**, wenn der Oszillator von unten durch seine Signallinie kreuzt.

Die Positionsrichtung folgt immer dem letzten Kreuzungssignal, während der Flat-Filter das Trading in Seitwärtsbewegungen verhindert.
