# Wie man Leverage und Margin einsetzt — Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ein Crossover-System mit dem Stochastic-Oszillator. Die Strategie kauft, wenn die %K-Linie die %D-Linie unterhalb von 80 von unten kreuzt, und verkauft leer, wenn %K die %D-Linie oberhalb von 20 von oben kreuzt. Positionen werden durch einen in Ticks gemessenen Take‑Profit geschützt.

## Details

- **Einstiegskriterien**:
  - **Long**: %K kreuzt %D von unten und %K < 80.
  - **Short**: %K kreuzt %D von oben und %K > 20.
- **Long/Short**: Beide
- **Ausstiegskriterien**: Take‑Profit oder entgegengesetzter Crossover
- **Stops**: Ja, Take‑Profit in Ticks
- **Standardwerte**:
  - `Stochastic Period` = 13
  - `%K Period` = 4
  - `%D Period` = 3
  - `Take Profit Ticks` = 100
  - `CandleType` = 1 Minute
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Stochastic
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
