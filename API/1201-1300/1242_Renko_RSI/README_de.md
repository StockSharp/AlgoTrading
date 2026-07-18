# Strategie Renko RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Renko-Bausteine anhand von RSI-Überkauft-/Überverkauft-Signalen handelt.

Tests zeigen moderate Performance und funktioniert am besten auf Märkten mit klaren Renko-Trends.

Renko RSI verwendet Renko-Bausteine aus dem ATR und wendet einen kurzen RSI an. Ein Kreuzen über das überverkaufte Niveau löst einen Kauf aus, während ein Fallen unter das überkaufte Niveau einen Verkauf auslöst.

## Details

- **Einstiegskriterien**: RSI kreuzt überverkaufte oder überkaufte Niveaus.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `RenkoAtrLength` = 14
  - `RsiLength` = 2
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `CandleType` = Renko ATR(14)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI, Renko
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Renko
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
