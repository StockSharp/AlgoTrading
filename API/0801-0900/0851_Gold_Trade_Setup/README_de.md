# Gold-Handelsaufbau-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Kaufman Adaptive Moving Average und SuperTrend.
Verkauft, wenn AMA steigt und SuperTrend in einen Aufwärtstrend wechselt.
Kauft, wenn AMA fällt und SuperTrend in einen Abwärtstrend wechselt.

## Details

- **Einstiegskriterien**: AMA-Richtung mit SuperTrend-Wechsel.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Feste Ziel- und Stop-Niveaus.
- **Stops**: Ja.
- **Standardwerte**:
  - `AmaLength` = 14
  - `FastLength` = 2
  - `SlowLength` = 30
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `TargetMultiplier` = 3.0
  - `RiskMultiplier` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: KAMA, SuperTrend
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
