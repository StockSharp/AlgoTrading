# Multi-Timeframe-Trendfolge mit 200 EMA-Filter - Nur Long
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie geht long, wenn die schnelle EMA über der langsamen EMA auf 5-, 15- und 30-Minuten-Charts liegt und der Kurs über der 200 EMA auf dem 5-Minuten-Chart liegt. Die Position wird geschlossen, wenn ein Zeitrahmen bärisch wird oder der Kurs unter die 200 EMA fällt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schnelle EMA > Langsame EMA auf 5-, 15- und 30-Minuten-Zeitrahmen und Schlusskurs > 200 EMA (5m).
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - Trend eines beliebigen Zeitrahmens wird negativ oder Schlusskurs < 200 EMA (5m).
- **Stops**:
  - Stop-Loss: Prozentsatz.
  - Take-Profit: Prozentsatz.
- **Standardwerte**:
  - `Fast EMA Length` = 9
  - `Slow EMA Length` = 21
  - `200 EMA Length` = 200
  - `Stop Loss %` = 1
  - `Take Profit %` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Basis 5m mit Bestätigung 15m und 30m
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
