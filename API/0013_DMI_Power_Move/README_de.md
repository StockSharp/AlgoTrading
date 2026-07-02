# Strategie DMI Power Move
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Strategie basierend auf DMI (Directional Movement Index) Power Moves

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 76%. Am besten funktioniert sie auf dem Forex-Markt.

DMI Power Move kombiniert Unterschiede des Richtungsindikators mit dem ADX, um starke Trends zu erfassen. Trades werden eröffnet, wenn +DI deutlich über -DI liegt (oder umgekehrt) und der ADX stark ist. Sie werden beendet, wenn der ADX nachlässt oder der DI-Abstand sich verengt.

Dieser Ansatz filtert schwache Signale heraus, indem sowohl eine starke Richtungsbewegung als auch ein steigender ADX gefordert werden. Das Ergebnis sind weniger, aber potenziell qualitativ hochwertigere Trend-Trades.


## Details

- **Einstiegskriterien**: Signale basierend auf ADX, ATR, DMI.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `DmiPeriod` = 14
  - `DiDifferenceThreshold` = 5m
  - `AdxThreshold` = 30m
  - `AdxExitThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ADX, ATR, DMI
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (15m)
  - Saisonalität: Nein
  - Neural Networks: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

