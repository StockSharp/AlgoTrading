# Parabolic RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Parabolic SAR auf den RSI anwendet, um Trendwechsel zu erkennen. Der Einstieg erfolgt, wenn der SAR relativ zur RSI-Linie umkehrt; Trades können optional über RSI-Schwellenwerte gefiltert werden.

## Details

- **Einstiegskriterien**:
  - Long: `SAR` kippt unter den RSI und (optional) `RSI ≥ LongRsiMin`
  - Short: `SAR` kippt über den RSI und (optional) `RSI ≤ ShortRsiMax`
- **Long/Short**: Konfigurierbar
- **Ausstiegskriterien**: Gegenläufiger SAR-Flip
- **Stops**: Keine
- **Standardwerte**:
  - `RsiLength` = 14
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `LongRsiMin` = 50
  - `ShortRsiMax` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Konfigurierbar
  - Indikatoren: Parabolic SAR, RSI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
