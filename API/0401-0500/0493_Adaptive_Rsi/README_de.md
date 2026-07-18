# Adaptive RSI-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die adaptive RSI-Strategie leitet einen Glättungskoeffizienten aus dem Relative Strength Index ab. Wenn der RSI vom neutralen Niveau 50 abweicht, erhöht sich der Koeffizient, sodass der adaptive RSI dem Kurs enger folgt. In der Nähe von 50 schrumpft der Koeffizient und die Kurve glättet sich. Eine Long-Position wird eröffnet, wenn der adaptive RSI nach oben dreht, während eine Short-Position eröffnet wird, wenn er nach unten dreht.

## Details

- **Einstiegskriterien**:
  - Adaptiver RSI kreuzt seinen vorherigen Wert nach oben.
  - Adaptiver RSI kreuzt seinen vorherigen Wert nach unten.
- **Long/Short**: Sowohl Long- als auch Short-Trades.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Length` = 14
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: RSI
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
