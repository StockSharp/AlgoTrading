# Bias-Ratio-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Bias-Ratio-Strategie handelt Ausbrüche basierend auf der Preisabweichung von langfristigen gleitenden Durchschnitten. Sie vergleicht den Schlusskurs sowohl mit einem exponentiellen gleitenden Durchschnitt (EMA) als auch mit einem einfachen gleitenden Durchschnitt (SMA). Eine Long-Position wird eröffnet, wenn der Preis die EMA um ein bestimmtes Verhältnis übersteigt, und eine Short-Position, wenn der Preis um dasselbe Verhältnis unter die SMA fällt.

## Details

- **Einstiegskriterien**:
  - `close / EMA >= 1 + BiasThreshold` → Long einsteigen.
  - `close / SMA <= 1 - BiasThreshold` → Short einsteigen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Das entgegengesetzte Signal schließt und kehrt Positionen um.
- **Stops**: Keine.
- **Standardwerte**:
  - `MaPeriod` = 200
  - `BiasThreshold` = 0.025
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long & Short
  - Indikatoren: EMA, SMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
