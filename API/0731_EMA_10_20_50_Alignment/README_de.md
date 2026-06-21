# EMA 10/20/50 Ausrichtungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Nur-Long-Strategie steigt ein, wenn EMA(10) > EMA(20) > EMA(50) ist, und steigt aus, wenn die EMAs in absteigender Reihenfolge ausgerichtet sind. Der Handel ist auf einen konfigurierbaren Datumsbereich beschränkt.

## Details

- **Einstiegskriterien**: EMA(10) über EMA(20) über EMA(50) innerhalb des angegebenen Datumsbereichs.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: EMAs richten sich abwärts aus (EMA(10) < EMA(20) < EMA(50)).
- **Stops**: Nein.
- **Standardwerte**:
  - `StartTime` = new DateTimeOffset(2023, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `EndTime` = new DateTimeOffset(2025, 5, 17, 0, 0, 0, TimeSpan.Zero)
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Long
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
