# Stochastic RSI OHLC-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie baut OHLC-Balken aus dem Stochastic RSI-Indikator auf und handelt bei Momentum-Wechseln. Sie berechnet den RSI für Hoch-, Tief- und Schlusskurse und wendet einen stochastischen Oszillator auf jede Reihe an. Eine Long-Position öffnet sich, wenn der Stochastic RSI von einem Pivot ansteigt und über das Long-Einstiegsniveau kreuzt. Eine Short-Position öffnet sich, wenn er von einem Pivot fällt und unter das Short-Einstiegsniveau kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Stochastic RSI dreht nach oben und einer der letzten drei Werte überschreitet `LongEntry` nach einem Tief-Pivot.
  - **Short**: Stochastic RSI dreht nach unten und einer der letzten drei Werte fällt unter `ShortEntry` nach einem Hoch-Pivot.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `RSI Length` = 14
  - `K Length` = 14
  - `D Length` = 3
  - `LongEntry` = 30
  - `ShortEntry` = 60
  - `LongPivot` = 2
  - `ShortPivot` = 98
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI, Stochastic
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
