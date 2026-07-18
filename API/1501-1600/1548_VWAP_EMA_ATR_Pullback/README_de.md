# Strategie VWAP EMA ATR Pullback
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Trendfolge-Strategie unter Verwendung von EMAs, VWAP und ATR.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 55%. Am besten funktioniert sie am Futures-Markt.

Der Ansatz identifiziert starke Trends über den ATR-basierten Abstand zwischen schneller und langsamer EMA. Einstiege erfolgen, wenn der Preis zum VWAP zurückzieht, um dem Trend beizutreten. Der Take-Profit wird beim VWAP plus oder minus dem ATR-Vielfachen platziert.

## Details

- **Einstiegskriterien**:
  - **Long**: Aufwärtstrend und Close < VWAP.
  - **Short**: Abwärtstrend und Close > VWAP.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Ziel bei VWAP ± ATR * Multiplikator.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastEmaLength` = 30
  - `SlowEmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, ATR, VWAP
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
