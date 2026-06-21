# Bollinger RSI Gegentrend-Strategie SOL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gegentrend-System für SOL, das kauft, wenn der Kurs das untere Bollinger Band nach oben kreuzt und der RSI niedrig ist, und verkauft, wenn der Kurs das obere Band nach unten kreuzt und der RSI hoch ist. Nur an Wochentagen.

## Details

- **Einstiegskriterien**:
  - **Long**: Kurs kreuzt das untere Band nach oben und `RSI` < `Long RSI` an Wochentagen.
  - **Short**: Kurs kreuzt das obere Band nach unten und `RSI` > `Short RSI` an Wochentagen.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Long: Kurs kreuzt das obere Band nach oben oder Stop Loss unter den jüngsten Tiefs.
  - Short: Kurs kreuzt das mittlere Band nach oben oder erreicht das Gewinnziel.
- **Stops**: Long-Stop unterhalb der jüngsten Tiefs.
- **Standardwerte**:
  - `Bollinger Period` = 20
  - `Bollinger Width` = 2
  - `RSI Length` = 14
  - `Long RSI` = 25
  - `Short RSI` = 79
  - `Short Profit %` = 3.5
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja (Wochentage)
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
