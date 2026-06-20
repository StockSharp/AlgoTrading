# Adx Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie, die den ADX (Average Directional Index) für die Trendstärke und den Stochastic Oszillator für das Einstiegs-Timing mit überkauften/überverkauften Bedingungen kombiniert.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 172%. Sie funktioniert am besten im Forexmarkt.

ADX hebt die Trendstärke hervor, während Stochastic Pullbacks identifiziert. Long- oder Short-Signale erscheinen, wenn sich der Impuls dreht, solange ADX hoch bleibt.

Es eignet sich für Trader, die Trendfolge mit Oszillator-Timing kombinieren. Schützende ATR-Stops helfen, Drawdowns zu kontrollieren.

## Details

- **Einstiegskriterien**:
  - Long: `ADX > AdxThreshold && StochK < StochOversold && Bullish`
  - Short: `ADX > AdxThreshold && StochK > StochOverbought && Bearish`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Ausstieg wenn `ADX < AdxThreshold`
- **Stops**: Prozentbasiert bei `StopLossPercent`
- **Standardwerte**:
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
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
  - Indikatoren: ADX, Stochastic Oscillator
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

