# Verfeinerte SMA EMA Crossover-Strategie mit Ichimoku und 200 SMA Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Kombiniert einen kurzen SMA/EMA-Crossover mit Ichimoku Cloud und 200-Perioden-SMA-Filtern. Geht long, wenn SMA über EMA kreuzt, über der Cloud und über der 200 SMA liegt. Verkauft, wenn SMA unter EMA kreuzt, unter der Cloud und der 200 SMA liegt.

## Details

- **Einstiegskriterien:**
  - **Long:** SMA kreuzt über EMA, Preis über Ichimoku Cloud, Preis über 200 SMA.
  - **Short:** SMA kreuzt unter EMA, Preis unter Ichimoku Cloud, Preis unter 200 SMA.
- **Ausstiegskriterien:** umgekehrtes Signal.
- **Indikatoren:** Ichimoku Cloud, SMA, EMA, 200 SMA.
