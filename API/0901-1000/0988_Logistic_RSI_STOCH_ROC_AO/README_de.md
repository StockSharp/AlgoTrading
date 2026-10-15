# Strategie Logistic RSI STOCH ROC AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie wendet eine logistische Abbildung auf einen ausgewählten Indikator (AO, ROC, RSI, Stochastic) an und handelt, wenn die vorzeichenbehaftete Standardabweichung die Null kreuzt.

## Details

- **Einstiegskriterien**: Vorzeichenbehaftete Standardabweichung kreuzt über null.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Vorzeichenbehaftete Standardabweichung kreuzt unter null.
- **Stops**: Keine.
- **Standardwerte**:
  - `Indicator` = LogisticDominance
  - `Length` = 13
  - `LenLd` = 5
  - `LenRoc` = 9
  - `LenRsi` = 14
  - `LenSto` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: AwesomeOscillator, RateOfChange, RelativeStrengthIndex, StochasticOscillator, Highest
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
