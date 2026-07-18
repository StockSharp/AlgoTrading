# Squeeze Pro Overlays-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Squeeze Pro Overlays-Strategie erkennt Volatilitätskontraktionen, wenn die Bollinger Bands vollständig innerhalb mehrerer Keltner Channels liegen. Sobald der Squeeze endet, bestimmt die Steigung einer linearen Regression auf Schlusskursen die Handelsrichtung.

## Details

- **Einstiegskriterien**:
  - Squeeze endet (Bollinger Bands bewegen sich außerhalb des breitesten Keltner Channels).
  - **Long**: Momentum-Steigung > 0.
  - **Short**: Momentum-Steigung < 0.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `SqueezeLength` = 20
- **Filter**:
  - Kategorie: Volatilität Ausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Keltner Channels, Linear Regression
  - Stops: Keine
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
