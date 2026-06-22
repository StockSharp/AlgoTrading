# Trix Candle-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Umkehrungen basierend auf dem Trix Candle-Indikator, der einen dreifachen exponentiellen gleitenden Durchschnitt auf die Eröffnungs- und Schlusskurse der Kerzen anwendet und jede Kerze einfärbt, je nachdem ob der geglättete Schlusskurs über oder unter der geglätteten Eröffnung liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: vorherige Kerze bullisch (Farbe 2) und aktuelle Kerzenfarbe < 2
  - **Short**: vorherige Kerze bärisch (Farbe 0) und aktuelle Kerzenfarbe > 0
- **Long/Short**: Long und Short
- **Ausstiegskriterien**:
  - Long: vorherige Kerze bärisch (Farbe 0)
  - Short: vorherige Kerze bullisch (Farbe 2)
- **Stops**: Nein
- **Standardwerte**:
  - `TRIX Period` = 14
  - `Candle Type` = 4h
  - `Allow Buy Open` = true
  - `Allow Sell Open` = true
  - `Allow Buy Close` = true
  - `Allow Sell Close` = true
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Triple Exponential Moving Average
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
