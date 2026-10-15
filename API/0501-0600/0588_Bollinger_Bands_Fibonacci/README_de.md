# Bollinger Bands & Fibonacci-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Bollinger Band-Ausbrüche, gefiltert durch Fibonacci-Levels. Eine Long-Position öffnet sich, wenn der Preis über das obere Band kreuzt und das Kerzentief über einem Fibonacci-basierten Support liegt. Eine Short-Position öffnet sich, wenn der Preis unter das untere Band fällt und das Kerzenhoch unter einem Fibonacci-basierten Widerstand liegt.

## Details

- **Einstiegskriterien**:
  - **Long**: Close kreuzt über das obere Band und Tief > Fibonacci-Tief.
  - **Short**: Close kreuzt unter das untere Band und Hoch < Fibonacci-Hoch.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Close kreuzt unter das mittlere Band.
  - **Short**: Close kreuzt über das mittlere Band.
- **Stops**: Keine.
- **Standardwerte**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2
  - `FibonacciLength` = 50
  - `FibonacciLevel0` = 0
  - `FibonacciLevel100` = 1
- **Filter**:
  - Kategorie: Bandausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Highest, Lowest
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: 1H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
