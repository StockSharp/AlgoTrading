# Warrior Trading Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Von Warrior Trading inspirierte Momentum-Strategie, die Gap-Erkennung, VWAP und Red-to-Green-Setups kombiniert.

## Details

- **Einstiegskriterien**: Gap-and-go, Red-to-Green oder VWAP-Abprall mit Volumen-Spike.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: ATR-basierter Stop, Take-Profit und Trailing.
- **Stops**: Ja.
- **Standardwerte**:
  - `GapThreshold` = 2m
  - `GapVolumeMultiplier` = 2m
  - `VwapDistance` = 0.5m
  - `MinRedCandles` = 3
  - `RiskRewardRatio` = 2m
  - `TrailingStopTrigger` = 1m
  - `MaxDailyTrades` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Long
  - Indikatoren: VWAP, RSI, EMA, ATR, Volume
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch
