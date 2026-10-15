# MACD RSI EMA BB ATR Tageshandel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Intraday-Strategie, die MACD-Signalkreuzung, RSI-Grenzen und EMA-Trendrichtung mit einem Bollinger-Bänder-Squeeze-Filter kombiniert. Das Risikomanagement verwendet ATR-basierten Stop-Loss, Trailing Stop und Risk-Reward-Take-Profit.

## Details

- **Einstiegskriterien**: MACD kreuzt Signal in Trendrichtung, RSI innerhalb der Schwellenwerte und kein BB-Squeeze.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzter Stop oder Ziel.
- **Stops**: ATR-basierter Stop-Loss, Trailing Stop und Risk-Reward-Take-Profit.
- **Standardwerte**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `EmaFast` = 9
  - `EmaSlow` = 21
  - `AtrLength` = 14
  - `AtrMultiplier` = 2.0
  - `TrailAtrMultiplier` = 1.5
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `RiskReward` = 2.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MACD, RSI, EMA, Bollinger Bands, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
