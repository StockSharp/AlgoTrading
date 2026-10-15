# Vegas Tunnel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet vier EMAs zur Definition eines Tunnels und optionale ATR-basierte Stops.
Öffnet Long, wenn Preis und schnelle EMA über den langsamen und Tunnel-EMAs liegen, Short wenn darunter.

## Details

- **Einstiegskriterien**: Ausrichtung der EMAs mit dem Preis relativ zum Tunnel
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder Take-Profit
- **Stops**: ATR- oder EMA-basiert
- **Standardwerte**:
  - `RiskRewardRatio` = 2
  - `UseAtr` = true
  - `AtrLength` = 14
  - `AtrMult` = 1.5
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
