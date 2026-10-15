# Diagramm-Oszillator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt mit einem auswählbaren Oszillator. Wählen Sie zwischen Stochastic, RSI oder MFI. Sie kauft, wenn der Oszillator überverkaufte Bedingungen signalisiert, und verkauft bei überkauften Bedingungen. Für die Stochastic-Option werden Signale über %K- und %D-Kreuzungen generiert.

Tests zeigen gute Performance auf volatilen Märkten wie Kryptowährungen.

Positionen drehen um, wenn entgegengesetzte Bedingungen erscheinen oder der Stop-Loss ausgelöst wird.

## Details

- **Einstiegskriterien**: Überverkaufte/überkaufte Niveaus des Oszillators und %K/%D-Kreuzungen.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensignal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `Choice` = OscillatorChoice.Stochastic
  - `Length` = 14
  - `KPeriod` = 14
  - `DPeriod` = 3
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: Stochastic/RSI/MFI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
