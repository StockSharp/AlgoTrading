# Go Candle Body Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Go-Indikator, der die Kerzenkörpergröße mittelt. Sie eröffnet eine Long-Position, wenn der geglättete Kerzenkörper nach einer positiven Phase unter null kreuzt, und eine Short-Position beim entgegengesetzten Kreuzungsvorgang. Bestehende Positionen werden bei entgegengesetzten Signalen geschlossen.

## Details

- **Einstiegskriterien**: Vorzeichenwechsel des Körper-SMA (positiv zu negativ für Long, negativ zu positiv für Short)
- **Long/Short**: Beide
- **Ausstiegskriterien**: entgegengesetzter Vorzeichenwechsel des Körper-SMA
- **Stops**: Nein
- **Standardwerte**:
  - `Period` = 174
  - `CandleType` = 1 Stunde
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Long & Short
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
