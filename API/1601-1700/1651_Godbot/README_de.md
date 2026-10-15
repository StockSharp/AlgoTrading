# Godbot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt mit Bollinger Bands kombiniert mit gleitenden Durchschnitten, um Umkehrungen und Trendstärke zu erkennen.

## Logik
- Arbeitet auf einem Haupt-Kerzenzeitrahmen (Standard 30 Minuten).
- Berechnet Bollinger Bands und eine EMA auf diesem Zeitrahmen.
- Berechnet separat eine DEMA auf einem höheren Zeitrahmen (Standard 1 Tag) zur Bestimmung des globalen Trends.
- Schließt Long-Positionen, wenn der Preis unter das obere Bollinger-Band zurückfällt.
- Schließt Short-Positionen, wenn der Preis über das untere Bollinger-Band zurücksteigt.
- Eröffnet Long, wenn der Preis das untere Band von unten kreuzt, während sowohl DEMA als auch EMA steigen.
- Eröffnet Short, wenn der Preis das obere Band von oben kreuzt, während sowohl DEMA als auch EMA fallen.

## Parameter
- **Bollinger Period** – Periode der Bollinger Bands.
- **Bollinger Deviation** – Breitenmultiplikator für die Bänder.
- **EMA Period** – Periode für den EMA-Trendfilter.
- **DEMA Period** – Periode für die DEMA auf dem höheren Zeitrahmen.
- **Candle Type** – Zeitrahmen für Bollinger-Bands- und EMA-Berechnungen.
- **DEMA Candle Type** – Höherer Zeitrahmen für die DEMA.

## Hinweise
- Es wird immer nur eine Position gehalten.
- Die Strategie verwendet Marktorders für Ein- und Ausstiege.
- DEMA-Daten müssen sich ansammeln, bevor der Handel beginnt.
