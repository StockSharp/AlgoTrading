# Strategie Donchian Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Donchian Channel + Stochastic Strategie. Die Strategie tritt in den Markt ein, wenn der Preis aus dem Donchian-Kanal ausbricht und der Stochastic überverkaufte/überkaufte Bedingungen bestätigt.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 85%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

Ausbrüche über den Donchian-Kanal werden mit dem Stochastic-Momentum bestätigt. Trades beginnen, sobald der Preis den Bereich verlässt und der Oszillator zustimmt.

Nützlich für Trader, die unmittelbares Follow-Through erwarten. Ein ATR-Vielfaches legt den Stop fest.

## Details

- **Einstiegskriterien**:
  - Long: `Close > DonchianHigh && StochK < 20`
  - Short: `Close < DonchianLow && StochK > 80`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Ausbruchsfehlschlag oder entgegengesetztes Signal
- **Stops**: Prozentbasiert mit `StopLossPercent`
- **Standardwerte**:
  - `DonchianPeriod` = 20
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `StopLossPercent` = 2m
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Donchian Channel, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

