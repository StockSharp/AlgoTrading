# Ichimoku by FarmerBTC-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ichimoku by FarmerBTC eröffnet Long-Positionen, wenn der Preis über der Ichimoku-Wolke liegt, die Wolke bullisch ist, ein SMA eines höheren Zeitrahmens den Aufwärtstrend bestätigt und das Volumen seinen gleitenden Durchschnitt multipliziert mit einem Faktor überschreitet. Der Ausstieg erfolgt, wenn der Preis unter die Wolke fällt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: entgegengesetztes Signal
- **Stops**: Nein
- **Standardwerte**:
  - `TenkanPeriod` = 10
  - `KijunPeriod` = 30
  - `SenkouSpanBPeriod` = 53
  - `SmaLength` = 13
  - `VolumeLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CandleType` = 1 hour
  - `HtfCandleType` = 1 day
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: Ichimoku, SMA, Volumen
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
