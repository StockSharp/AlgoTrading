# Doppelte Keltner-Kanäle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie **Doppelte Keltner-Kanäle** verwendet zwei Keltner-Kanäle mit unterschiedlichen Multiplikatoren, um Ausbrüche zu erkennen.
Ein Trade wird eröffnet, wenn der Preis das äußere Band durchbricht und dann durch das innere Band zurückkehrt.
Stops und Ziele werden mit festen Prozentsätzen verwaltet.

## Details
- **Einstiegskriterien**: Preis kreuzt das äußere Keltner-Band und kreuzt das innere Band in dieselbe Richtung zurück.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop-Loss, Take-Profit oder entgegengesetztes Signal.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `EmaPeriod = 50`
  - `InnerMultiplier = 2.75m`
  - `OuterMultiplier = 3.75m`
  - `MaxStopPercent = 10m`
  - `SlTpRatio = 1m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keltner
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
