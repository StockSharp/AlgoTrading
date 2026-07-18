# MACD Momentum-Umkehr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet das MACD-Histogramm, um Momentum-Umkehrungen zu erkennen.
Geht short, wenn eine bullische Kerze wächst, aber das MACD-Histogramm sinkt.
Kauft, wenn eine bärische Kerze wächst, aber das MACD-Histogramm steigt.

## Details

- **Einstiegskriterien**: Größerer Kerzenkörper bei nachlassendem MACD-Momentum.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
