# Exp QqeCloud-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein trendfolgendes Konzept, das den QQE-Indikator (Quantitative Qualitative Estimation) auf einen geglätteten RSI anwendet.
Die Strategie eröffnet Positionen nur zu einer vordefinierten Sitzungsstartzeit und schließt sie, wenn das entgegengesetzte Signal auftritt
oder die Handelssitzung endet.

## Details

- **Einstiegskriterien**:
  - **Long**: Zu `StartHour`:`StartMinute` dreht der QQE-Trend nach oben.
  - **Short**: Zu `StartHour`:`StartMinute` dreht der QQE-Trend nach unten.
- **Ausstiegskriterien**:
  - Entgegengesetztes QQE-Trendsignal.
  - Zeit überschreitet `StopHour`:`StopMinute`.
- **Indikatoren**:
  - RSI (Periode `RsiPeriod`, geglättet durch `RsiSmoothing`).
  - QQE-Bänder mit Multiplikator `QqeFactor`.
- **Stops**: Standardmäßig keine.
- **Standardwerte**:
  - `CandleType` = 1-Minuten-Kerzen
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.236
  - `StartHour` = 0, `StartMinute` = 0
  - `StopHour` = 23, `StopMinute` = 59
- **Filter**:
  - Zeitfenster für Einstiege und Ausstiege
  - Trendfolge, einzelner Zeitrahmen
