# Delta SMA 1-Jahres-Hoch-Tief-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Delta SMA 1-Jahres-Hoch-Tief**-Strategie berechnet das Volumendelta (Kaufvolumen minus Verkaufsvolumen) und seinen einfachen gleitenden Durchschnitt. Sie geht Long, wenn das Delta-SMA sehr niedrig war und dann über null kreuzt. Die Position wird geschlossen, wenn das Delta-SMA unter 60% seines 1-Jahres-Hochs fällt, nachdem es zuvor über 70% dieses Hochs gekreuzt hat.

## Details
- **Einstiegskriterien**: Das Delta-SMA lag unter 70% seines 1-Jahres-Tiefs und kreuzt über null.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Das Delta-SMA fällt unter 60% seines 1-Jahres-Hochs, nachdem es 70% gekreuzt hat.
- **Stops**: Nein.
- **Standardwerte**:
  - `DeltaSmaLength = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Long
  - Indikatoren: SMA, Highest, Lowest
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
