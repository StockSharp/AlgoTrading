# SMA Trendfilter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Multi-Zeitrahmen-Strategie, die die Steigung von fünf einfachen gleitenden Durchschnitten (Perioden 5, 8, 13, 21, 34) auf drei Zeitrahmen (15m, 1h, 4h) analysiert. Sie berechnet bullische und bärische Punktzahlen für jeden Zeitrahmen und handelt, wenn alle Zeitrahmen in eine Richtung zeigen.

## Details

- **Einstiegskriterien**:
  - Long: Alle drei Zeitrahmen zeigen, dass mindestens 50% der SMAs steigen
  - Short: Alle drei Zeitrahmen zeigen, dass mindestens 50% der SMAs fallen
- **Long/Short**: Beide
- **Ausstiegskriterien**: Entgegengesetztes Signal basierend auf dem Schlusskursniveau
- **Stops**: Nein
- **Standardwerte**:
  - `OpenLevel` = 0
  - `CloseLevel` = 0
  - `CandleType1` = TimeSpan.FromMinutes(15).TimeFrame()
  - `CandleType2` = TimeSpan.FromHours(1).TimeFrame()
  - `CandleType3` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
