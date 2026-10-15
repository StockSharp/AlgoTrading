# Volatilitäts-Arbitrage-Spread-Oszillator-Modell (VASOM)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Geht beim VIX-Front-Month-Future long, wenn der RSI des Spreads zwischen den Kontrakten des ersten und zweiten Monats unter einen Schwellenwert fällt. Die Position wird geschlossen, wenn der RSI über ein Ausstiegsniveau steigt.

## Details
- **Einstiegskriterien**: Spread-RSI < `LongThreshold`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Spread-RSI > `ExitThreshold`.
- **Stops**: Nein.
- **Standardwerte**:
  - `RsiPeriod` = 2
  - `LongThreshold` = 46
  - `ExitThreshold` = 76
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SecondSecurity` = "CBOE:VX2!"
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Nur Long
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
