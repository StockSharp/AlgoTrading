# Combo-Strategie 123 Reversal & Fractal Chaos Bands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die ein 123-Umkehrmuster mit einem Fractal Chaos Bands Ausbruch kombiniert.
Long-Trades entstehen, wenn eine bullische 123-Umkehr entsteht und der Preis über der oberen Fraktalband schließt.
Short-Trades entstehen, wenn eine bärische 123-Umkehr mit einem Schlusskurs unter der unteren Fraktalband zusammenfällt.

## Details

- **Einstiegskriterien**:
  - Long: Reversal123 Long-Signal und Schlusskurs über oberer Fraktalband.
  - Short: Reversal123 Short-Signal und Schlusskurs unter unterer Fraktalband.
- **Long/Short**: Beide
- **Ausstiegskriterien**: Gegensätzliches Signal
- **Stops**: Nein
- **Standardwerte**:
  - `Length` = 15
  - `KSmoothing` = 1
  - `DLength` = 3
  - `Level` = 50m
  - `Pattern` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Muster & Ausbruch
  - Richtung: Beide
  - Indikatoren: Stochastic Oscillator, Fractal Chaos Bands
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
