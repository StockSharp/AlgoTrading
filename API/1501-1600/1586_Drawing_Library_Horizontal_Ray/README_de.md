# Strategie mit horizontalem Strahl (Zeichenbibliothek)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Zeichnet horizontale Strahlen an SMA-Kreuzungspunkten und handelt in Richtung des Kreuzes.

## Details

- **Einstiegskriterien**: `SMA20` kreuzt `SMA50` nach oben für Long, nach unten für Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegenläufige Kreuzung.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `CandleType` = 5 minutes
- **Filter**:
  - Kategorie: Zeichnung
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
