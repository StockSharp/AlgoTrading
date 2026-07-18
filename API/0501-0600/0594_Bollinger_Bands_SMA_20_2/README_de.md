# Bollinger Bands SMA 20-2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet Bollinger Bands, die auf einem einfachen gleitenden Durchschnitt über 20 Perioden mit einem Multiplikator von 2 Standardabweichungen basieren. Sie geht long, wenn der Preis über das untere Band kreuzt, und short, wenn der Preis unter das obere Band kreuzt. Positionen werden bei entgegengesetzten Signalen umgekehrt, ohne explizite Stop Losses.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close` kreuzt über das untere Band.
  - **Short**: `Close` kreuzt unter das obere Band.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Einzeln
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
