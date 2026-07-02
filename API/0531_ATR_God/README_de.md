# Strategie ATR GOD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einen Supertrend-Einstieg mit ATR-basiertem Stop-Loss und Take-Profit kombiniert.

## Details

- **Einstiegskriterien**: Supertrend-Umkehr.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-Stop oder entgegengesetztes Signal.
- **Stops**: ATR-basiert.
- **Standardwerte**:
  - `Period` = 10
  - `Multiplier` = 3m
  - `RiskMultiplier` = 4.5m
  - `RewardRiskRatio` = 1.5m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, Supertrend
  - Stops: ATR
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

