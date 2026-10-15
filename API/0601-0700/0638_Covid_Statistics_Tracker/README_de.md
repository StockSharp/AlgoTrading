# Covid-Statistik-Tracker-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die auf der Wachstumsrate bestätigter COVID-19-Fälle handelt.
Die Strategie verkauft, wenn das Fallwachstum beschleunigt, und kauft, wenn das Wachstum sich verlangsamt.

## Details

- **Einstiegskriterien**:
  - Long: `growth < 1`
  - Short: `growth > 1`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensignal
- **Stops**: Nein
- **Standardwerte**:
  - `Region` = "US"
  - `Lookback` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Sonstige
  - Richtung: Beide
  - Indikatoren: Benutzerdefiniert
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
