# Bayesian BBSMA Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie schätzt die Wahrscheinlichkeit, dass die nächste Kerze nach oben oder unten ausbricht, mithilfe eines Bayesian-Modells, das auf Bollinger Bands und einem einfachen gleitenden Durchschnitt basiert. Eine optionale Bestätigung durch Bill Williams' Accelerator- und Alligator-Indikatoren kann Signale filtern. Wenn die Wahrscheinlichkeit eines Aufwärtsausbruchs den Schwellenwert überschreitet, wird ein Long-Trade eröffnet. Eine hohe Wahrscheinlichkeit eines Abwärtsausbruchs löst einen Short aus.

## Details

- **Einstiegskriterien**:
  - Long, wenn die primäre oder aufwärtsgerichtete Wahrscheinlichkeit über `LowerThreshold` (Standard 15%) steigt und, falls aktiviert, die Bill-Williams-Bestätigung bullish ist.
  - Short, wenn die primäre oder abwärtsgerichtete Wahrscheinlichkeit den Schwellenwert überschreitet und, falls aktiviert, die Bill-Williams-Bestätigung bearish ist.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Umgekehrtes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `BbSmaPeriod` = 20
  - `BbStdDevMult` = 2.5
  - `AoFast` = 5
  - `AoSlow` = 34
  - `AcFast` = 5
  - `SmaPeriod` = 20
  - `BayesPeriod` = 20
  - `LowerThreshold` = 15
  - `UseBwConfirmation` = false
  - `JawLength` = 13
- **Filter**:
  - Kategorie: Probabilistische Trendfolge
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, SMA, Awesome Oscillator, Accelerator Oscillator, Alligator
  - Stops: Nein
  - Komplexität: Hoch
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
