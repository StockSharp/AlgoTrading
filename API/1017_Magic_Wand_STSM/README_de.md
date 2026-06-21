# Magic Wand STSM Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Trendfolge-System, das den Supertrend-Indikator mit einem 200-Perioden-SMA-Filter verwendet. Es handelt in Richtung des Supertrends und nutzt die Linie als Stop, mit einem konfigurierbaren Risiko-Ertrags-Take-Profit.

## Details

- **Einstiegskriterien**:
  - **Long**: Supertrend unterhalb des Kurses und Schluss oberhalb SMA200.
  - **Short**: Supertrend oberhalb des Kurses und Schluss unterhalb SMA200.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Take-Profit bei `entry ± (entry - Supertrend) * RiskReward`.
  - Stop-Loss bei Supertrend.
- **Stops**: Ja.
- **Standardwerte**:
  - `Supertrend Period` = 10
  - `Supertrend Multiplier` = 3
  - `MA Length` = 200
  - `Risk Reward` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
