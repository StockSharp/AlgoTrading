# Strategie Supertrend Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Supertrend + Stochastic Strategie. Die Strategie eröffnet Trades, wenn Supertrend die Trendrichtung anzeigt und Stochastic mit überverkauften/überkauften Bedingungen bestätigt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 142%. Sie funktioniert am besten auf dem Aktienmarkt.

Supertrend markiert den Trend, und Stochastic weist auf temporäre Gegenbewegungen hin. Einstiege erfolgen, sobald Stochastic den überverkauften oder überkauften Bereich entgegen dem Trend verlässt.

Am besten für Momentum-Trader geeignet, die klare Trendsignale benötigen. ATR-Werte definieren den Stop-Abstand.

## Details

- **Einstiegskriterien**:
  - Long: `Close > Supertrend && StochK < 20`
  - Short: `Close < Supertrend && StochK > 80`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Supertrend-Umkehr
- **Stops**: Verwendet Supertrend als Trailing Stop
- **Standardwerte**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Supertrend, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

