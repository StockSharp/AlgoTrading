# Fractal Force Index-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis eines geglätteten Force Index, der benutzerdefinierte Niveaus kreuzt. Wenn der Indikator über das hohe Niveau steigt oder unter das niedrige Niveau fällt, öffnet oder schließt die Strategie Positionen je nach gewähltem Handelsmodus. Der Force Index wird aus der Preisänderung und dem Volumen berechnet und mit einer EMA geglättet.

## Details

- **Einstiegskriterien**
  - *Direkter Modus*:
    - **Long**: Indikator kreuzt `HighLevel` nach oben.
    - **Short**: Indikator kreuzt `LowLevel` nach unten.
  - *Gegen-Modus*:
    - **Long**: Indikator kreuzt `LowLevel` nach unten.
    - **Short**: Indikator kreuzt `HighLevel` nach oben.
- **Ausstiegskriterien**
  - *Direkter Modus*:
    - **Long**: Kreuzung unter `LowLevel`.
    - **Short**: Kreuzung über `HighLevel`.
  - *Gegen-Modus*:
    - **Long**: Kreuzung über `HighLevel`.
    - **Short**: Kreuzung unter `LowLevel`.
- **Stops**: Nein.
- **Standardwerte**:
  - `Period` = 30
  - `HighLevel` = 0
  - `LowLevel` = 0
  - `Candle Type` = 4-hour
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Force Index
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
