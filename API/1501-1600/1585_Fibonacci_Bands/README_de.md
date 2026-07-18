# Fibonacci-Bänder-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erweitert einen Keltner-Kanal um Fibonacci-Verhältnisse und handelt, wenn der Preis das äußere Band mit RSI-Bestätigung durchbricht.

## Details

- **Einstiegskriterien**: Preis kreuzt `fbUpper3` mit RSI über 60 für Long; kreuzt `fbLower3` mit RSI unter 40 für Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Preis kreuzt zurück über den gleitenden Durchschnitt.
- **Stops**: Nein.
- **Standardwerte**:
  - `MaType` = WMA
  - `MaLength` = 233
  - `Fib1` = 1.618
  - `Fib2` = 2.618
  - `Fib3` = 4.236
  - `KcMultiplier` = 2
  - `KcLength` = 89
  - `RsiLength` = 14
  - `CandleType` = 5 minutes
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: MA, ATR, RSI
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
