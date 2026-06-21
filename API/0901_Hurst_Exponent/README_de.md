# Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Einfache Strategie, die auf Basis eines geglätteten Hurst Exponent handelt.  
Der Hurst-Wert wird mit einer EMA geglättet und mit einem Schwellenwert verglichen, um das Marktregime zu bestimmen.

## Details
- **Einstiegskriterien**:
  - **Long**: Geglätteter Hurst > Schwellenwert
  - **Short**: Geglätteter Hurst < Schwellenwert
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: Geglätteter Hurst < Schwellenwert
  - **Short**: Geglätteter Hurst > Schwellenwert
- **Stops**: Ja, prozentualer Stop-Loss.
- **Standardwerte**:
  - `HurstPeriod = 100`
  - `SmoothLength = 10`
  - `Threshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5)`
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Hurst Exponent, EMA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
