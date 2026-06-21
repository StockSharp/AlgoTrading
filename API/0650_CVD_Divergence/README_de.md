# CVD-Divergenz-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert die Divergenz des kumulierten Volumen-Deltas (CVD) mit Hull Moving Averages, RSI, MACD und einem Volumenfilter. Ein Trade öffnet sich, wenn Trend, Momentum und Volumen übereinstimmen und das CVD Divergenz oder Fortsetzung in Handelsrichtung zeigt. Positionen schließen bei entgegengesetzten Signalen oder Indikatorkreuzungen.

## Details

- **Einstiegskriterien**: Trendausrichtung durch HMA, RSI- und MACD-Bestätigung, hohes Volumen und CVD-Divergenz/-Fortsetzung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Indikatorkreuzung.
- **Stops**: Keine expliziten Stops.
- **Standardwerte**:
  - `HmaFastLength` = 20
  - `HmaSlowLength` = 50
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeMaLength` = 20
  - `VolumeMultiplier` = 1.5m
  - `CvdLength` = 14
  - `DivergenceLookback` = 5
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Divergenz
  - Richtung: Beide
  - Indikatoren: HMA, RSI, MACD, Volumen
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
