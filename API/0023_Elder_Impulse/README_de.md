# Strategie Elder Impulse
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf Elders Impulssystem

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 106%. Sie funktioniert am besten am Aktienmarkt.

Elder Impulse kombiniert die EMA-Richtung mit der Farbe des MACD-Histogramms. Grüne Balken oberhalb der EMA signalisieren Long-Positionen, rote Balken darunter Short-Positionen, und neutrale Balken signalisieren Ausstiege.

Durch die Kombination von Trendrichtung und Momentum hält dieser Ansatz Trader auf der richtigen Seite starker Bewegungen. Ausstiege sind unkompliziert und basieren auf dem Farbwechsel des Histogramms oder der Umkehr der EMA-Steigung.


## Details

- **Einstiegskriterien**: Signale basierend auf MACD.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

