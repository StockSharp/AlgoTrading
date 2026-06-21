# MCOTs Intuition-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf RSI-Momentum relativ zu seiner Standardabweichung. Kauft, wenn das aufwärts gerichtete Momentum stark, aber nachlassend ist, und verkauft bei entgegengesetzten Bedingungen. Feste Gewinnziele und Stop-Loss werden in Ticks gesetzt.

## Details

- **Einstiegskriterien**:
  - Long: momentum > stdDev * multiplier und momentum < previousMomentum * exhaustionMultiplier
  - Short: momentum < -stdDev * multiplier und momentum > previousMomentum * exhaustionMultiplier
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Festes Gewinnziel und Stop-Loss in Ticks
- **Stops**: Ja
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `StdDevMultiplier` = 1m
  - `ExhaustionMultiplier` = 1m
  - `ProfitTargetTicks` = 40
  - `StopLossTicks` = 160
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: RSI, StandardDeviation
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
