# Geglättete Heiken-Ashi-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mit EMA geglättete Heiken-Ashi-Kerzen heben die Beschleunigung von Preisbewegungen hervor. Eine Long-Position wird eröffnet, wenn eine bullische geglättete Kerze einen größeren Körper als die vorherige hat. Die Position wird geschlossen, wenn sich der bärische Körper ausdehnt.

## Details

- **Einstiegskriterien**: bullische geglättete Heiken-Ashi-Kerze mit größerem Körper als die vorherige
- **Long/Short**: Long
- **Ausstiegskriterien**: bärischer Körper dehnt sich aus
- **Stops**: Nein
- **Standardwerte**:
  - `EmaLength` = 40
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long
  - Indikatoren: EMA, Heikin-Ashi
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
