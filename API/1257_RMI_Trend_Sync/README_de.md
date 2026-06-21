# RMI Trend Sync
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

RMI Trend Sync kombiniert RSI- und MFI-Momentum-Signale mit einem SuperTrend-Trailing-Stop. Ein Long-Trade öffnet, wenn das durchschnittliche Momentum einen Schwellenwert mit steigendem EMA-Gefälle überschreitet, während ein Short-Trade bei einem Abwärtsausbruch ausgelöst wird. SuperTrend liefert den Ausstiegs-Trail.

## Details

- **Einstiegskriterien**: Momentum-Durchschnitt kreuzt Schwellenwerte mit EMA-Neigungsbestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Momentum oder SuperTrend-Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `RmiLength` = 21
  - `PositiveThreshold` = 70
  - `NegativeThreshold` = 30
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3.5
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: RSI, MFI, EMA, SuperTrend
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
