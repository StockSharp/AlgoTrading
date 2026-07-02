# Strategie Rsi Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die RSI und Stochastic Oszillator zur doppelten Bestätigung von überverkauften und überkauften Bedingungen kombiniert.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 181%. Sie funktioniert am besten im Kryptomarkt.

Der RSI bietet eine breitere Momentumansicht, während Stochastic schnellere Signale nahe den Extremen gibt. Trades wechseln, wenn der Oszillator Levels innerhalb des RSI-Kontexts kreuzt.

Ideal für flinke Trader, die Oszillator-Setups bevorzugen. Die Strategie verlässt sich auf einen ATR-Stop zur Risikobegrenzung.

## Details

- **Einstiegskriterien**:
  - Long: `RSI < RsiOversold && StochK < StochOversold`
  - Short: `RSI > RsiOverbought && StochK > StochOverbought`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: `RSI > 50`
  - Short: `RSI < 50`
- **Stops**: Prozentbasiert bei `StopLossPercent`
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `StochOversold` = 20m
  - `StochOverbought` = 80m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

