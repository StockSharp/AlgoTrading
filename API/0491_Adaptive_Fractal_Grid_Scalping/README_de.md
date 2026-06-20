# Adaptives Fraktal-Grid-Scalping
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das adaptive Fraktal-Grid-Scalping platziert Limit-Orders rund um aktuelle Fraktal-Pivots und verwendet ATR für den Abstand. Der Trend wird durch einen einfachen gleitenden Durchschnitt definiert. Wenn die Volatilität einen Schwellenwert überschreitet, werden in Aufwärtstrends Kauf-Limits unterhalb der fraktalen Tiefs und in Abwärtstrends Verkauf-Limits oberhalb der fraktalen Hochs gesetzt. Ausstiege erfolgen beim gegenüberliegenden Grid-Level oder über einen ATR-basierten Trailing-Stop.

## Details

- **Einstiegskriterien**: ATR über Schwellenwert mit dem Kurs relativ zur SMA; Kauf-Limit am fraktalen Tief minus ATR-Multiplikator oder Verkauf-Limit am fraktalen Hoch plus ATR-Multiplikator.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegenüberliegendes Grid-Level oder fraktalbasierter Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrLength` = 14
  - `SmaLength` = 50
  - `GridMultiplierHigh` = 2.0m
  - `GridMultiplierLow` = 0.5m
  - `TrailStopMultiplier` = 0.5m
  - `VolatilityThreshold` = 1.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Scalping
  - Richtung: Beide
  - Indikatoren: Fractal, ATR, SMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
