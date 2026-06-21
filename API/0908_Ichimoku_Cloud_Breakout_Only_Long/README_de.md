# Ichimoku Cloud Ausbruch Nur Long
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet Long-Positionen, wenn der Preis über die Ichimoku-Wolke ausbricht, und schließt sie, wenn der Preis wieder darunter fällt. Es werden ausschließlich Long-Trades eingegangen.

## Details

- **Einstiegskriterien**:
  - Long: `Close` kreuzt über `max(SenkouA, SenkouB)`
- **Long/Short**: Nur Long
- **Ausstiegskriterien**:
  - `Close` kreuzt unter `min(SenkouA, SenkouB)`
- **Stops**: Keine
- **Standardwerte**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: Ichimoku
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
