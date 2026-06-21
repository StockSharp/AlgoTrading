# HMA 200 + EMA 20 Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet eine Long-Position, wenn der Preis über dem 200-Perioden Hull Moving Average
liegt und den 20-Perioden Exponential Moving Average von unten kreuzt. Short-Positionen werden
eröffnet, wenn der Preis unter der HMA liegt und die EMA von oben kreuzt. Positionen kehren sich
bei entgegengesetzten Signalen um.

## Details

- **Einstiegskriterien**:
  - **Long**: `Close > HMA` und `Close` kreuzt `EMA` von unten.
  - **Short**: `Close < HMA` und `Close` kreuzt `EMA` von oben.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Umkehr beim entgegengesetzten Crossover-Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `HMA Length` = 200
  - `EMA Length` = 20
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: HMA, EMA
  - Stops: Keine
  - Komplexität: Einfach
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
