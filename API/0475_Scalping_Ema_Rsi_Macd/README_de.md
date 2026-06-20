# Scalping EMA RSI MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

30-Minuten-Scalping-Strategie, die schnelles/langsames EMA-Crossover, Trend-EMA, RSI- und MACD-Filter mit einer Volumenbedingung kombiniert. Der Stop-Loss basiert auf ATR und der Take-Profit verwendet ein festes Risiko-Ertrags-Verhältnis.

## Details

- **Einstiegskriterien**: Schnelles EMA kreuzt langsames EMA in Trendrichtung, RSI innerhalb der Grenzen, MACD-Bestätigung und hohes Volumen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Stop oder Ziel erreicht.
- **Stops**: ATR-basierter Stop-Loss und Risiko-Ertrags-Take-Profit.
- **Standardwerte**:
  - `FastEmaLength` = 12
  - `SlowEmaLength` = 26
  - `TrendEmaLength` = 55
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `VolumeMaLength` = 20
  - `VolumeThreshold` = 1.3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: EMA, RSI, MACD, ATR, Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (30m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
