# Intelligente Grid-Scalping-Pullback-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Grid-basierte Scalping-Strategie, die ATR-gesteuerte Preisniveaus von einem Basispreis zwanzig Bars zurück erweitert. Pullbacks werden vor dem Einstieg mit RSI gefiltert. Positionen verwenden ein Gewinnziel und einen ATR-Trailing-Stop.

## Details

- **Einstiegskriterien**:
  - Long: close < basePrice - (LongLevel + 1) * ATR * GridFactor && range/low > NoTradeZone && RSI < MaxRsiLong && close > open
  - Short: close > basePrice + (ShortLevel + 1) * ATR * GridFactor && range/high > NoTradeZone && RSI > MinRsiShort && close < open
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gewinnziel oder ATR-Trailing-Stop
- **Stops**: ATR-Trailing-Stop
- **Standardwerte**:
  - `AtrLength` = 10
  - `GridFactor` = 0.35m
  - `ProfitTarget` = 0.004m
  - `NoTradeZone` = 0.003m
  - `ShortLevel` = 5
  - `LongLevel` = 5
  - `MinRsiShort` = 70
  - `MaxRsiLong` = 30
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: ATR, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
