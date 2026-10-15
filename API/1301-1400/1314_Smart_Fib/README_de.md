# Smart Fib-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die einen Ausbruch des einfachen gleitenden Durchschnitts für Einstiege und ATR-basierte Fibonacci-Bänder für Ausstiege verwendet.

## Details

- **Einstiegskriterien**: Schlusskurs kreuzt den SMA von unten oder oben.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis erreicht das ATR-Fibonacci-Band.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SmaLength` = 50
  - `FibSmaLength` = 8
  - `AtrLength` = 6
  - `FirstFactor` = 1.618
  - `SecondFactor` = 2.618
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, ATR
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
