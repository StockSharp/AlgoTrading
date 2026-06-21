# Escort Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Escort Trend-Strategie kombiniert einen schnellen und langsamen gewichteten gleitenden Durchschnitt (WMA) mit MACD- und CCI-Bestätigung. Eine Long-Position wird eröffnet, wenn die schnelle WMA über der langsamen WMA liegt, die MACD-Hauptlinie die Signallinie von unten kreuzt und der CCI einen positiven Schwellenwert überschreitet. Eine Short-Position wird bei entgegengesetzten Bedingungen ausgelöst. Die Strategie verwendet optional einen festen Stop-Loss, Take-Profit und Trailing-Stop.

## Details
- **Einstiegskriterien**:
  - **Long**: `FastWMA > SlowWMA` UND `MACD > Signal` UND `CCI > +Threshold`.
  - **Short**: `FastWMA < SlowWMA` UND `MACD < Signal` UND `CCI < -Threshold`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Einstiegssignal.
  - Optionaler Stop-Loss, Take-Profit oder Trailing-Stop.
- **Stops**: Ja, benutzerdefiniert.
- **Standardwerte**:
  - `Fast WMA` = 8
  - `Slow WMA` = 18
  - `CCI Period` = 14
  - `CCI Threshold` = 100
  - `MACD Fast EMA` = 8
  - `MACD Slow EMA` = 18
  - `Take Profit` = 200
  - `Stop Loss` = 55
  - `Trailing Stop` = 35
  - `Trailing Step` = 3
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
