# Pin Bar Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet Pin Bar Kerzen mit einem Trendfilter und ATR-basierten Stops und Zielen. Ein bullischer Pin Bar oberhalb des SMA eröffnet eine Long-Position, ein bärischer unterhalb eine Short-Position. Einstiege werden übersprungen, wenn die Volatilität zu niedrig ist.

## Details

- **Einstiegskriterien**: Pin Bar in Trendrichtung mit langer Lunte, kleinem Körper und ATR über `MinAtr`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: ATR-basierter Stop-Loss oder Take-Profit.
- **Stops**: Ja, ATR-Vielfache.
- **Standardwerte**:
  - `TrendLength` = 50
  - `MaxBodyPct` = 0.30
  - `MinWickPct` = 0.66
  - `AtrLength` = 14
  - `StopMultiplier` = 1
  - `TakeMultiplier` = 1.5
  - `MinAtr` = 0.0015
  - `CandleType` = 1 hour
- **Filter**:
  - Kategorie: Muster
  - Richtung: Beide
  - Indikatoren: SMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
