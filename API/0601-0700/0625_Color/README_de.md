# Farb-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die basierend auf der wahrgenommenen Helligkeit einer konfigurierten Farbe handelt.
Wenn die Farbe hell ist (Luminanz > 0,5), kauft die Strategie, andernfalls verkauft sie.

## Details

- **Einstiegskriterien**:
  - Long: `Color luminance > 0.5`
  - Short: `Color luminance <= 0.5`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensätzliches Signal
- **Stops**: Nein
- **Standardwerte**:
  - `ColorHex` = "#f23645"
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filter**:
  - Kategorie: Sonstige
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
