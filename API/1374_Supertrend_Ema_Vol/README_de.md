# Supertrend EMA Volumen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Supertrend mit EMA-Trendbestätigung und Volumenfilter kombiniert. Einstieg bei Supertrend-Umkehrungen, wenn der Preis über oder unter der EMA liegt und das Volumen seinen EMA überschreitet. Implementiert ATR-basierten Stop-Loss.

## Details

- **Einstiegskriterien**:
  - Long: Supertrend dreht nach oben, Preis über EMA, Volumen über Volume EMA
  - Short: Supertrend dreht nach unten, Preis unter EMA, Volumen über Volume EMA
- **Long/Short**: Konfigurierbar
- **Ausstiegskriterien**: Supertrend-Umkehr oder ATR-basierter Stop-Loss
- **Stops**: ATR-Vielfaches
- **Standardwerte**:
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `EmaLength` = 21
  - `StartDate` = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero)
  - `AllowLong` = true
  - `AllowShort` = false
  - `SlMultiplier` = 2m
  - `UseVolumeFilter` = true
  - `VolumeEmaLength` = 20
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Supertrend, EMA, Volume EMA, ATR
  - Stops: ATR
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
