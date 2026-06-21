# Heatmap MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses System verwendet eine Heatmap von MACD-Histogrammen aus fünf Zeitrahmen. Wenn alle Histogramme über oder unter null wechseln, wird in der entsprechenden Richtung eingestiegen und ausgestiegen, sobald die Ausrichtung bricht oder Risikolimits ausgelöst werden.

## Details

- **Einstiegskriterien**: Alle MACD-Histogramme über/unter null.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Histogramm-Ausrichtung bricht oder Stops werden ausgelöst.
- **Stops**: Ja.
- **Standardwerte**:
  - `FastPeriod` = 20
  - `SlowPeriod` = 50
  - `SignalPeriod` = 50
  - `TakeProfitPercent` = 2
  - `StopLossPercent` = 2
  - `CandleType1` = TimeSpan.FromMinutes(60)
  - `CandleType2` = TimeSpan.FromMinutes(120)
  - `CandleType3` = TimeSpan.FromMinutes(240)
  - `CandleType4` = TimeSpan.FromMinutes(240)
  - `CandleType5` = TimeSpan.FromMinutes(480)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Multi-Zeitrahmen
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
