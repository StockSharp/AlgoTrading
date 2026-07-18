# Heikin Ashi V2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese zweite Version des Heikin Ashi-Systems fügt einen EMA-Filter hinzu. Trades werden nur dann ausgeführt, wenn die Richtung der Heikin Ashi-Kerze mit dem durch den EMA definierten Trend übereinstimmt. Der Filter hilft, Gegentrend-Signale zu vermeiden, die der reine HA-Ansatz generieren könnte.

## Details

- **Einstiegskriterien**:
  - **Long**: `HA_Close > HA_Open` und `Close > EMA`
  - **Short**: `HA_Close < HA_Open` und `Close < EMA`
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal
- **Stops**: Keine
- **Standardwerte**:
  - `EmaLength` = 20
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, EMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
