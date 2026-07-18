# Mehrfachbedingungen-Kurvenanpassungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert EMA-Crossover, RSI und Stochastik-Oszillator, um zu handeln, wenn mehrere Signale übereinstimmen.

## Details

- **Einstiegskriterien**:
  - Long: `FastEMA > SlowEMA` und `RSI < RsiOversold` und `StochK < 20`
  - Short: `FastEMA < SlowEMA` und `RSI > RsiOverbought` und `StochK > 80`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: `FastEMA < SlowEMA` oder `RSI > RsiOverbought` oder `StochK > StochD`
  - Short: `FastEMA > SlowEMA` oder `RSI < RsiOversold` oder `StochK < StochD`
- **Stops**: Keine
- **Standardwerte**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 25
  - `RsiLength` = 14
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `StochLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, RSI, Stochastic
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
