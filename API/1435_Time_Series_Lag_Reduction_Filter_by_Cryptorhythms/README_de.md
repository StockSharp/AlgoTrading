# Zeitreihen-Lag-Reduktionsfilter von Cryptorhythms
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem EMA-Lag-Reduktionsfilter.

Der Algorithmus vergleicht den Preis mit einer lag-bereinigten EMA und handelt bei Kreuzungen.

## Details

- **Einstiegskriterien**: Preis kreuzt die lag-reduzierte EMA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenläufige Kreuzung.
- **Stops**: Nein.
- **Standardwerte**:
  - `LagReduction` = 20m
  - `EmaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
