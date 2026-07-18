# Statistische Arbitrage-Spread-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt den Spread zwischen zwei korrelierten Instrumenten. Eine Long-Position im ersten Wertpapier wird eröffnet, wenn der Spread unter seinen Mittelwert um ein Vielfaches der Standardabweichung des Spreads fällt. Die Position wird geschlossen, sobald der Spread zum Mittelwert zurückkehrt.

## Details
- **Einstiegskriterien**:
  - Long: Spread < Mittelwert - Multiplikator * Standardabweichung
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Schließen wenn Spread > Mittelwert
- **Stops**: Nein
- **Standardwerte**:
  - `LookbackPeriod` = 20
  - `StdMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Arbitrage
  - Richtung: Long
  - Indikatoren: Spread-Statistiken
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
