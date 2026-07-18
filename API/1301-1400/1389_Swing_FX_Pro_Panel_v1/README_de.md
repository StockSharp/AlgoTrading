# Strategie Swing FX Pro Panel v1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Demonstrationsstrategie mit EMA-Crossover und grundlegenden Leistungsstatistiken. Der schnelle EMA, der den langsamen EMA von unten nach oben kreuzt, öffnet eine Long-Position; ein Kreuz nach unten öffnet eine Short-Position. Jeder Trade verwendet feste Gewinn- und Verlustzielen.

## Details

- **Indikatoren**: EMA
- **Parameter**:
  - `Initial Capital` – anfängliche Kontogröße für Statistiken.
  - `Risk Per Trade` – prozentuales Risiko pro Trade (informativ).
  - `Analysis Period` – Periodenlänge für die Analyse.
  - `Fast Length` – schnelle EMA-Periode.
  - `Slow Length` – langsame EMA-Periode.
  - `Profit Target` – Gewinn in Preiseinheiten.
  - `Stop Loss` – Verlust in Preiseinheiten.
