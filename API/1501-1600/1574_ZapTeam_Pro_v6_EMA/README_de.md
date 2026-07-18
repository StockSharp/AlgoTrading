# ZapTeam Pro v6-Strategie — EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Vereinfachte Version mit EMA21/EMA50-Kreuzung und EMA200-Trendfilter. Kauft bei bullischem Kreuzungssignal und verkauft bei bärischem (Shorts optional).

## Details

- **Einstiegskriterien**: EMA21 kreuzt EMA50 mit Trendfilter
- **Long/Short**: Beide (Shorts optional)
- **Ausstiegskriterien**: Gegenläufige Kreuzung
- **Stops**: Nein
- **Standardwerte**:
  - `Ema21Length` = 21
  - `Ema50Length` = 50
  - `Ema200Length` = 200
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
