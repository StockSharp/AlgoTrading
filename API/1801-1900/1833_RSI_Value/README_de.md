# Strategie RSI Value
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die auf dem Relative Strength Index (RSI) basiert, der einen mittleren Wert kreuzt.

Die Idee ist, darauf zu achten, wenn der RSI ein konfigurierbares Niveau (Standard 50) von unten oder oben kreuzt. Wenn der Indikator von unten nach oben über dieses Niveau steigt, wird eine Long-Position eröffnet. Wenn er wieder darunter kreuzt, wird eine Short-Position eröffnet. Bestehende Positionen werden beim entgegengesetzten Kreuz geschlossen. Optionaler Stop-Loss, Take-Profit und Trailing-Stop schützen den Trade.

## Details

- **Einstiegskriterien**: Kaufen, wenn RSI das Niveau nach oben kreuzt. Verkaufen, wenn RSI darunter kreuzt.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Kreuzung oder Trailing-Stop.
- **Stops**: Optionaler fester Stop-Loss, Take-Profit und Trailing-Stop.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `RsiLevel` = 50
  - `StopLoss` = 100
  - `TakeProfit` = 200
  - `TrailingStop` = 0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
