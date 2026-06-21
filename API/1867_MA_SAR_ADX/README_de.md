# MA SAR ADX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einen Moving Average, Parabolic SAR und den Average Directional Index (ADX) kombiniert.
Kauft, wenn der Preis über dem gleitenden Durchschnitt und dem SAR liegt und +DI über -DI ist.
Verkauft, wenn der Preis unter dem gleitenden Durchschnitt und dem SAR liegt und +DI unter -DI ist.
Positionen werden geschlossen, wenn der Preis den SAR kreuzt.

## Details

- **Einstiegskriterien**:
  - Long: `Close > MA && +DI >= -DI && Close > SAR`
  - Short: `Close < MA && +DI <= -DI && Close < SAR`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Preis kreuzt Parabolic SAR
- **Stops**: Nein
- **Standardwerte**:
  - `MaPeriod` = 100
  - `AdxPeriod` = 14
  - `SarStep` = 0.02m
  - `SarMax` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, Parabolic SAR, ADX
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
