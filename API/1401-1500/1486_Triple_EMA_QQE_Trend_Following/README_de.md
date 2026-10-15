# Triple EMA + QQE Trendfolge-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie, die zwei TEMA-Linien mit einem QQE-Filter kombiniert.
Long-Positionen werden eröffnet, wenn der Preis über beiden TEMA-Linien liegt und QQE ein bullisches Signal gibt.
Short-Positionen werden bei entgegengesetzten Bedingungen eröffnet.
Ein Trailing-Stop in Punkten schützt offene Positionen.

## Details

- **Einstiegskriterien**: TEMA-Ausrichtung mit QQE-Kreuzung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensignal oder Trailing-Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238m
  - `Tema1Length` = 20
  - `Tema2Length` = 40
  - `StopLossPips` = 120
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, QQE
  - Stops: Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
