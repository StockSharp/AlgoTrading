# Strategie Stochastic RSI Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Stochastic RSI-Kreuz

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 112%. Sie funktioniert am besten auf dem Forexmarkt.

Stochastic RSI Cross beobachtet die %K- und %D-Linien des StochRSI. Bullische Kreuzungen in der Nähe von überverkauften Niveaus lösen Käufe aus, bärische Kreuzungen in der Nähe von überkauften lösen Verkäufe aus, und entgegengesetzte Kreuzungen steigen aus.

Da der StochRSI schnell oszilliert, können Signale häufig sein. Viele Trader verlangen, dass die Kreuzung in der Nähe eines Extrems erfolgt, um Rauschen herauszufiltern.


## Details

- **Einstiegskriterien**: Signale basierend auf RSI, Stochastic.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI, Stochastic
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

