# Parabolic SAR Frühkauf-Strategie mit MA-Ausstieg
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Parabolic SAR-Umkehrungen und schließt Long-Positionen frühzeitig, wenn der SAR über den Kurs kippt und der Schlusskurs unter einem gleitenden Durchschnitt mit N Perioden liegt.

## Details

- **Einstiegskriterien**:
  - Kreuzung des Kurses mit dem Parabolic SAR.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Bei Long-Positionen: SAR über dem Kurs und Schlusskurs unter dem MA (`MaPeriod`).
  - Bei Short-Positionen: gegenläufige SAR-Kreuzung (durch die Einstiegslogik gesteuert).
- **Stops**: Keine.
- **Standardwerte**:
  - `SarStart` = 0.02
  - `SarIncrement` = 0.02
  - `SarMax` = 0.2
  - `MaPeriod` = 11
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: Parabolic SAR, SMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
