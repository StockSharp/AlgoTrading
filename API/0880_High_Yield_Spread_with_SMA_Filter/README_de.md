# High-Yield-Spread-Strategie mit SMA-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt auf Basis des High-Yield-Spreads oder des VIX-Index. Eine Position wird eröffnet, wenn der gewählte Spread einen Schwellenwert überschreitet und ein optionaler Preisfilter dies bestätigt. Der Preisfilter erfordert, dass der Schlusskurs für Longs über einem einfachen gleitenden Durchschnitt liegt oder für Shorts darunter. Positionen werden nach einer festen Anzahl von Bars geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: Spread > Schwellenwert und Schlusskurs > SMA (wenn aktiviert).
  - **Short**: Spread < Schwellenwert und Schlusskurs < SMA (wenn aktiviert).
- **Long/Short**: Beide, per Parameter wählbar.
- **Ausstiegskriterien**:
  - Position nach Halteperiode-Bars schließen.
- **Stops**: Keine.
- **Standardwerte**:
  - `Threshold` = 5
  - `HoldingPeriod` = 5
  - `SmaLength` = 50
- **Filter**:
  - Kategorie: Makro
  - Richtung: Beide
  - Indikatoren: High Yield Spread/VIX, SMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: 1d (Standard)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
