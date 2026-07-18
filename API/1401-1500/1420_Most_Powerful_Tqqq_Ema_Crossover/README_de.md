# Leistungsstärkste TQQQ EMA-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie geht long, wenn eine schnelle EMA eine langsame EMA nach oben kreuzt. Take-Profit und Stop-Loss werden als Multiplikatoren des Einstiegspreises festgelegt.

## Details

- **Einstiegskriterien**: Schnelle EMA kreuzt langsame EMA nach oben
- **Long/Short**: Nur Long
- **Ausstiegskriterien**: Preis erreicht Take-Profit- oder Stop-Loss-Niveau
- **Stops**: Ja (fester Multiplikator)
- **Standardwerte**:
  - `FastLength` = 20
  - `SlowLength` = 50
  - `TakeProfitMultiplier` = 1.3
  - `StopLossMultiplier` = 0.95
- **Filter**:
  - Kategorie: Trend
  - Richtung: Nur Long
  - Indikatoren: EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
