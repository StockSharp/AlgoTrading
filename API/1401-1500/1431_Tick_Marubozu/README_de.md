# Tick-Marubozu-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erkennt Marubozu-Kerzen in Tick-Daten und bestätigt sie mit hohem Volumen. Kauft bei bullischen Marubozu und verkauft bei bärischen.

## Details

- **Einstiegskriterien**: Bullisches oder bärisches Marubozu mit Volumen über dem SMA
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensignal
- **Stops**: Nein
- **Standardwerte**:
  - `TickSize` = 5
  - `VolLength` = 20
  - `CandleType` = 1-minute time frame
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
