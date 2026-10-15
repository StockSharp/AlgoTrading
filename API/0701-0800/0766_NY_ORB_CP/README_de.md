# NY ORB CP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

NY Opening Range Breakout-Strategie mit Retest-Bestätigung. Handelt Ausbrüche der NY-Range von 9:30-9:45, wenn der Preis einen Retest durchführt und die Ausbruchsrichtung fortsetzt.

## Details

- **Einstiegskriterien**:
  - Long: Der Preis retestet das NY-Hoch nach dem Ausbruch mit Trend- und Volumenbestätigung.
  - Short: Der Preis retestet das NY-Tief nach dem Ausbruch nach unten mit Trend- und Volumenbestätigung.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Gewinnziel bei 0.33 der Range * `RiskReward`.
  - Stop Loss bei 0.33 der Range.
- **Stops**: Ja, dynamisch.
- **Standardwerte**:
  - `MinRangePoints` = 60
  - `RiskReward` = 3
  - `MaxTradesPerSession` = 3
  - `MaxDailyLoss` = -1000
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: EMA, VWAP, SMA
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
