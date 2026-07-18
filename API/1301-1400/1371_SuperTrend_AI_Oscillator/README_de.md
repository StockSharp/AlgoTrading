# SuperTrend AI Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

SuperTrend AI Oscillator kombiniert einen SuperTrend-Trailing-Stop mit einem benutzerdefinierten Oszillator-Filter.
Die Strategie handelt bei SuperTrend-Umkehrungen, die durch den Oszillator bestätigt werden.
Positionen werden durch einen Trailing-Stop und ein optionales Risiko-Gewinn-Ziel verwaltet.

## Details

- **Einstiegskriterien**: SuperTrend-Wechsel mit Oszillator > 50 für Long oder < 50 für Short
- **Long/Short**: Beide
- **Ausstiegskriterien**: Trailing-Stop oder Risiko-Gewinn-Take-Profit
- **Stops**: Trailing
- **Standardwerte**:
  - `AtrLength` = 10
  - `Factor` = 1
  - `RiskReward` = 2
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, Stochastic
  - Stops: Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
