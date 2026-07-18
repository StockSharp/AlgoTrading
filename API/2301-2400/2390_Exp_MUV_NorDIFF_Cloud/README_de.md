# Strategie Exp MUV NorDIFF Cloud
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem normalisierten Schwung von SMA und EMA.
Einstieg Long, wenn der SMA- oder EMA-Schwung +100 erreicht, und Short, wenn er -100 erreicht.

## Parameter
- `MaPeriod` – Periode der gleitenden Durchschnitte.
- `MomentumPeriod` – Anzahl der Bars für die Schwungberechnung.
- `KPeriod` – Fenster zur Normalisierung der Schwungextremen.
- `CandleType` – Zeitrahmen der Kerzen.

## Hinweise
Die Strategie berechnet SMA- und EMA-Werte, misst deren Schwung und normalisiert ihn innerhalb des jüngsten Bereichs, um Handelssignale zu generieren.
