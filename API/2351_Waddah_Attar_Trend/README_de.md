# Strategie Waddah Attar Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den originalen MQL-Experten "Exp_Waddah_Attar_Trend" in die StockSharp High-Level-API. Sie verwendet den Waddah Attar Trend-Indikator, der die Differenz zwischen zwei exponentiellen gleitenden Durchschnitten (schnell und langsam) mit einem zusätzlichen glättenden gleitenden Durchschnitt multipliziert. Der Indikator gibt einen Farbzustand aus: grün wenn der Trendwert steigt und magenta wenn er fällt. Eine Änderung dieser Farbe löst Trades aus.

Long-Positionen werden eröffnet, wenn die Farbe von abwärts auf aufwärts wechselt. Short-Positionen werden eröffnet, wenn sie von aufwärts auf abwärts wechselt. Die Strategie arbeitet in beide Richtungen und unterstützt schützende Stop-Loss- und Take-Profit-Werte als Prozentsätze des Einstiegspreises.

## Details

- **Einstiegskriterien**: Farbwechsel des Waddah Attar Trend (MACD-Differenz multipliziert mit MA).
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Farbwechsel oder Schutz-Stops.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `MaLength` = 9
  - `SignalBar` = 1
  - `TrendMode` = Direct
  - `StopLossPercent` = 1.0
  - `TakeProfitPercent` = 2.0
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: H4
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
