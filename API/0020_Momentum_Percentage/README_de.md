# Momentum Percentage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf prozentualem Preismomentum

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 97%. Am besten funktioniert sie auf dem Kryptomarkt.

Momentum Percentage verfolgt die prozentuale Preisveränderung. Trades werden ausgelöst, wenn das Momentum positive oder negative Niveaus überschreitet, und enden bei einem Gegensignal oder einem Volatilitätsstopp.

Durch die Messung von Renditen über einen festgelegten Rückblickzeitraum passt sich das System verschiedenen Märkten an. Der Volatilitätsstopp sorgt dafür, dass große negative Bewegungen schnell beendet werden.


## Details

- **Einstiegskriterien**: Signale basierend auf MA, Momentum.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MomentumPeriod` = 10
  - `ThresholdPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MA, Momentum
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

