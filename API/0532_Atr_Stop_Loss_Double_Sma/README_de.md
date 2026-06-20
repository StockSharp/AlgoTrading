# ATR Stop-Loss Doppel-SMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie geht Long, wenn ein schneller Simple Moving Average (SMA) einen langsamen SMA nach oben kreuzt, und Short beim umgekehrten Kreuz.
Ein optionaler Stop-Loss nutzt den Average True Range (ATR), multipliziert mit einem benutzerdefinierten Faktor, um die Ausstiegsniveaus zu bestimmen.

## Details

- **Einstiegskriterien**:
  - **Long**: Schneller SMA kreuzt über den langsamen SMA.
  - **Short**: Schneller SMA kreuzt unter den langsamen SMA.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - ATR-basierter Stop-Loss, wenn aktiviert.
- **Stops**: ATR-Vielfaches vom Einstiegspreis.
- **Standardwerte**:
  - `FastLength` = 15
  - `SlowLength` = 45
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, ATR
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
