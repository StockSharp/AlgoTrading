# RSI Bollinger Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den Relative Strength Index (RSI) mit Bollinger Bands kombiniert. Eine Long-Position wird eröffnet, wenn der RSI unter dem Überverkauft-Schwellenwert liegt und der Schlusskurs unter dem unteren Bollinger Band ist. Eine Short-Position wird eröffnet, wenn der RSI über dem Überkauft-Schwellenwert liegt und der Schlusskurs über dem oberen Bollinger Band ist. Positionen kehren sich bei entgegengesetzten Signalen um.

## Details

- **Einstiegskriterien**: RSI unter `RsiOversold` und Schlusskurs unter dem unteren Band für Kauf; RSI über `RsiOverbought` und Schlusskurs über dem oberen Band für Verkauf.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Umgekehrtes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `RsiPeriod` = 20
  - `BollingerPeriod` = 20
  - `BollingerWidth` = 2
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
- **Filter**:
  - Kategorie: Oszillator
  - Richtung: Beide
  - Indikatoren: RSI, Bollinger Bands
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: 15 Minuten
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
