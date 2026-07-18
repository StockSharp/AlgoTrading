# TripleStochasticMTF-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie betreibt drei Stochastic Oscillatoren auf verschiedenen Zeitrahmen und handelt, wenn der kleinste Zeitrahmen seine Signallinie in der durch die höheren Zeitrahmen bestätigten Richtung kreuzt. Sie ist darauf ausgelegt, kurzfristige Umkehrungen im Kontext eines übergeordneten Trends zu erfassen.

Der primäre Zeitrahmen (Standard 30 Minuten) und der sekundäre Zeitrahmen (Standard 15 Minuten) bestimmen die Marktausrichtung. Der Einstiegszeitrahmen (Standard 5 Minuten) wartet auf einen %K- und %D-Crossover entgegengesetzt zur vorherigen Bar, was einen Pullback signalisiert. Positionen werden geschlossen, wenn einer der überwachten Zeitrahmen einen Trendwechsel gegen den aktiven Trade signalisiert.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorheriges %K > %D im 5-Minuten-Chart, aktuelles %K ≤ %D, und beide höheren Zeitrahmen zeigen %K > %D.
  - **Short**: Vorheriges %K < %D im 5-Minuten-Chart, aktuelles %K ≥ %D, und beide höheren Zeitrahmen zeigen %K < %D.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Beliebiger Zeitrahmen wechselt zu Abwärtstrend (%K < %D).
  - **Short**: Beliebiger Zeitrahmen wechselt zu Aufwärtstrend (%K > %D).
- **Stops**: Standardmäßig nicht implementiert.
- **Standardwerte**:
  - `Timeframe 1` = 30 Minuten.
  - `Timeframe 2` = 15 Minuten.
  - `Timeframe 3` = 5 Minuten.
  - `%K Period` = 5.
  - `%D Period` = 3.
  - `Slowing` = 3.
- **Filter**:
  - Kategorie: Trendfolge / Pullback
  - Richtung: Beide
  - Indikatoren: Stochastic Oscillator
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
