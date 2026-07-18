# Bollinger Band-Berührung mit SMI- und MACD-Winkel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kauft, wenn der Preis das untere Bollinger Band berührt und sowohl SMI- als auch MACD-Winkel nach oben zeigen. Die Position wird geschlossen, sobald der Preis das obere Bollinger Band erreicht.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Schlusskurs berührt das untere Bollinger Band oder fällt darunter, und SMI/MACD-Winkel sind positiv, aber unter ihren Schwellenwerten.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - **Long**: Der Schlusskurs berührt das obere Bollinger Band oder überschreitet es.
- **Stops**: Keine.
- **Standardwerte**:
  - `BollingerLength` = 20
  - `BollingerMultiplier` = 2.0
  - `SmiLength` = 14
  - `SmiSignalLength` = 3
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `SmiAngleThreshold` = 60
  - `MacdAngleThreshold` = 50
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Nur Long
  - Indikatoren: Bollinger Bands, Stochastic (SMI), MACD
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: 1H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
