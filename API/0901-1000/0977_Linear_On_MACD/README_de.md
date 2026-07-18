# Lineare Strategie auf MACD-Basis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die MACD-Signale auf Preis und Volumen mit linearer Regression kombiniert.

## Details

- **Einstiegskriterien**: Long, wenn beide MACDs über ihren Signalen liegen und der Regressionspreis zwischen Eröffnung und Schlusskurs liegt; Short bei umgekehrten Bedingungen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `Lookback` = 21
  - `RiskHigh` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD, Linear Regression
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Variabel
