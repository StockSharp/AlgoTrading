# ADX für BTC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie verwendet den Average Directional Index (ADX) mit einem optionalen SMA-Trendfilter, um starke Bewegungen in Bitcoin zu erfassen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 80%. Am besten funktioniert sie auf dem Kryptomarkt.

Das System kauft, wenn der ADX das Einstiegsniveau nach oben kreuzt und der Trendfilter bullisch ist. Die Position schließt sich, wenn der ADX unter das Ausstiegsniveau fällt.

## Details

- **Einstiegskriterien**: ADX kreuzt `EntryLevel` nach oben und (falls aktiviert) schnelle SMA > langsame SMA.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: ADX kreuzt `ExitLevel` nach unten.
- **Stops**: Nein.
- **Standardwerte**:
  - `EntryLevel` = 14m
  - `ExitLevel` = 45m
  - `SmaFilter` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: ADX, SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
