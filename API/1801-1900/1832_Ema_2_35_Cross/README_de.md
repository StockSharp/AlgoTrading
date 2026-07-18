# EMA 2-35 Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie folgt einer einfachen Kreuzung zwischen zwei Exponential Moving Averages. Die schnelle EMA mit Länge 2 reagiert schnell auf Preisänderungen, während die langsame EMA mit Länge 35 den langfristigen Trend darstellt. Eine Position wird eröffnet, wenn die schnelle EMA die langsame EMA kreuzt; Positionen werden umgekehrt, wenn die entgegengesetzte Kreuzung auftritt.

Das Risikomanagement erfolgt mit festen Stop-Loss- und Take-Profit-Levels, ausgedrückt in Preisschritten. Außerdem wird ein Trailing-Stop angewendet, um Gewinne zu sichern, wenn sich der Trade in eine günstige Richtung bewegt.

## Details

- **Einstiegskriterien**:
  - **Long**: EMA(2) kreuzt EMA(35) nach oben.
  - **Short**: EMA(2) kreuzt EMA(35) nach unten.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetzte Kreuzung.
  - Stop-Loss oder Take-Profit erreicht.
  - Trailing-Stop ausgelöst.
- **Stops**: Fester Stop-Loss, Take-Profit und Trailing-Stop (alle in Preisschritten).
- **Standardwerte**:
  - `FastLength` = 2
  - `SlowLength` = 35
  - `StopLoss` = 50
  - `TakeProfit` = 150
  - `TrailingStop` = 50
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Gleitende Durchschnitte
  - Stops: Ja
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig

