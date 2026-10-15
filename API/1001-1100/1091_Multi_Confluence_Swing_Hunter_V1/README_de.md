# Multi-Confluenz Swing Hunter V1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Multi-Confluenz Swing Hunter V1 Strategie verwendet ein Punktesystem, das RSI, MACD und Preisaktionen kombiniert, um Swing-Tiefs und -Hochs zu identifizieren. Ein Long-Trade wird eröffnet, wenn bullische Signale den minimalen Einstiegswert erreichen, und geschlossen, wenn bärische Signale den Ausstiegswert erreichen.

## Details

- **Einstiegskriterien**: Einstiegspunktzahl ≥ `MinEntryScore` aus RSI/MACD-Signalen und bullischer Kerzenstruktur.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Ausstiegspunktzahl ≥ `MinExitScore` aus RSI/MACD-Signalen und bärischer Kerzenstruktur.
- **Stops**: Nein.
- **Standardwerte**:
  - `MacdFast` = 3
  - `MacdSlow` = 10
  - `MacdSignal` = 3
  - `RsiLength` = 21
  - `MinEntryScore` = 13
  - `MinExitScore` = 13
  - `MinLowerWickPercent` = 50
  - `RsiOversold` = 30
  - `RsiExtremeOversold` = 25
  - `RsiOverbought` = 70
  - `RsiExtremeOverbought` = 75
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Nur Long
  - Indikatoren: RSI, MACD
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
