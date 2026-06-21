# Asimmetric Stoch NR-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf asymmetrischen Stochastik-Oszillator-Linien. Die Strategie reagiert auf %K- und %D-Kreuzungen und unterstützt optionalen Positionsschutz.

Die Methode wechselt Perioden für die %K-Berechnung, um sich an das Marktrauschen anzupassen. Stop-Loss und Take-Profit werden in absoluten Preiseinheiten angewendet.

## Details

- **Einstiegskriterien**:
  - Long: `%K` kreuzt `%D` nach oben
  - Short: `%K` kreuzt `%D` nach unten
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: `%K` kreuzt `%D` nach unten
  - Short: `%K` kreuzt `%D` nach oben
- **Stops**: absolut bei `StopLoss` und `TakeProfit`
- **Standardwerte**:
  - `KPeriodShort` = 5
  - `KPeriodLong` = 12
  - `DPeriod` = 7
  - `Slowing` = 3
  - `Overbought` = 80m
  - `Oversold` = 20m
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Langfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
