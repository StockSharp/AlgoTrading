# Gleichgewichts-Kerzen-Muster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Gleichgewichtskerzen verwendet, um kurze Trends zu erkennen und bei Rücksetzern einzusteigen. Das Gleichgewicht ist der Mittelpunkt zwischen dem höchsten und niedrigsten Preis über ein Rückblickfenster. Nach einer bullischen oder bärischen Serie löst eine Bewegung zurück durch das Gleichgewicht einen Einstieg aus. ATR wird für optionale Stop/Ziel-Werte und zum Ausstieg bei ungewöhnlich großen Kerzen verwendet.

## Details

- **Einstiegskriterien**:
  - **Long**: Nach einem bullischen Trend, wenn der Preis das Gleichgewicht nach unten durchbricht.
  - **Short**: Nach einem bärischen Trend, wenn der Preis das Gleichgewicht nach oben durchbricht.
- **Long/Short**: Beide
- **Stops**: ATR-basierter Stop Loss und Take Profit (optional)
- **Standardwerte**:
  - `EquilibriumLength` = 9
  - `CandlesForTrend` = 7
  - `MaxPullbackCandles` = 2
  - `AtrPeriod` = 14
  - `StopMultiplier` = 2
  - `UseTpSl` = true
  - `UseBigCandleExit` = true
  - `BigCandleMultiplier` = 1
  - `UseReverse` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
