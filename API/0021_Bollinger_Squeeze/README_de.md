# Bollinger Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf Bollinger Bands Squeeze

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 100%. Am besten funktioniert sie auf dem Forex-Markt.

Bollinger Squeeze wartet auf eine enge Bandbreite, die geringe Volatilität anzeigt. Ein Ausbruch außerhalb der Bänder eröffnet einen Trade in diese Richtung, der aussteigt, wenn das Momentum nachlässt oder ein entgegengesetzter Ausbruch erscheint.

Die Squeeze-Bedingung deutet auf eine bevorstehende Volatilitätsexpansion hin. Einmal ausgelöst, reitet der Trade den Ausbruch und verlässt sich auf einen ATR-Stop oder Band-Crossover zum Ausstieg.


## Details

- **Einstiegskriterien**: Signale basierend auf Bollinger.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `SqueezeThreshold` = 0.1m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Bollinger
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

