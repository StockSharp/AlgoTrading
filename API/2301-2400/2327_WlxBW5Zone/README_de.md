# Wlx BW5 Zone-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Awesome Oscillator (AO) und den Accelerator Oscillator (AC) von Bill Williams, um starke Momentum-Sequenzen zu identifizieren. Ein Kauf- (Verkaufs-)signal erscheint, wenn beide Oszillatoren fünf aufeinanderfolgende Balken steigen (fallen). Das System dreht oder öffnet Positionen entsprechend.

## Details

- **Einstiegskriterien**:
  - **Long**: `AO` und `AC` steigen fünf aufeinanderfolgende Balken.
  - **Short**: `AO` und `AC` fallen fünf aufeinanderfolgende Balken.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Position bei entgegengesetztem Signal umkehren.
- **Stops**: Nein.
- **Standardwerte**:
  - `Timeframe` = 4 Stunden.
  - `Direct` = true.
  - `SignalBar` = 1.
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
