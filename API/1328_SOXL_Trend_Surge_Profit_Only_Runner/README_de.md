# SOXL Trend-Surge Nur-Gewinn-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet Long-Trades, wenn der Preis über der 200 EMA trendet und SuperTrend bullisch ist. Sie erfordert einen steigenden ATR, ein über dem Durchschnitt liegendes Volumen, einen Sessionsfilter und dass der Preis außerhalb eines kleinen EMA-Puffers liegt. Das System nimmt bei einem ATR-basierten Ziel teilweise Gewinne mit und verfolgt die verbleibende Position mit einem ATR-Stop.

## Details

- **Einstiegskriterien**: Preis über EMA, SuperTrend aufwärts, Volumen über Durchschnitt, ATR steigend, außerhalb EMA-Puffer, Zeit zwischen 14–19 Uhr, Abkühlung nach Ausstiegen
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: 50% Teilgewinnmitnahme am ATR-Ziel und Trailing Stop für den Rest
- **Stops**: Trailing
- **Standardwerte**:
  - `EmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultTarget` = 2.0
  - `CooldownBars` = 15
  - `SupertrendFactor` = 3.0
  - `SupertrendAtrPeriod` = 10
  - `MinBarsHeld` = 2
  - `VolFilterLen` = 20
  - `EmaBuffer` = 0.005
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: EMA, ATR, SuperTrend, Volumen
  - Stops: Trailing
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
