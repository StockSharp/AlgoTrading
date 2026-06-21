# Simulator Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die EMA-Kreuzungen mit optionalem Stop-Loss und Take-Profit handelt. Sie kauft, wenn der schnelle EMA den langsamen EMA von unten kreuzt, und verkauft, wenn der schnelle EMA den langsamen EMA von oben kreuzt. Gegenläufige Signale oder Preisziele schließen Positionen.

## Details

- **Einstiegskriterien**:
  - Long: Schneller EMA kreuzt langsamen EMA von unten
  - Short: Schneller EMA kreuzt langsamen EMA von oben
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetzter EMA-Kreuzung
  - Long: Kurs erreicht Take-Profit oder Stop-Loss
  - Short: Kurs erreicht Take-Profit oder Stop-Loss
- **Stops**: Feste Preisoffsets
- **Standardwerte**:
  - `FastPeriod` = 13
  - `SlowPeriod` = 50
  - `StopLoss` = 0.005m
  - `TakeProfit` = 0.005m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
