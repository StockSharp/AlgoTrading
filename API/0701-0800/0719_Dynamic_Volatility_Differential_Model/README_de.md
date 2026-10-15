# Dynamisches Volatilitätsdifferenzial-Modell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Dynamic Volatility Differential Model (DVDM)**-Strategie vergleicht implizite mit historischer Volatilität. Sie geht long, wenn die implizite Volatilität die realisierte Volatilität um einen dynamischen Standardabweichungsschwellenwert übersteigt, und short, wenn der Spread unter den negativen Schwellenwert fällt.

Signale nutzen Tagesdaten und basieren nicht auf Stops.

## Details
- **Einstiegskriterien**: Volatilitäts-Spread über/unter dynamischen Standardabweichungsschwellenwerten.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Volatilitäts-Spread kreuzt die Nulllinie.
- **Stops**: Nein.
- **Standardwerte**:
  - `Length = 5`
  - `StdevMultiplier = 7.1m`
  - `VolatilitySecurity = "TVC:VIX"`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Volatilität
  - Richtung: Beide
  - Indikatoren: StandardDeviation
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
