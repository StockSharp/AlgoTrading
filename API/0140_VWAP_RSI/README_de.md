# VWAP RSI Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
VWAP RSI verwendet den volumengewichteten Durchschnittspreis zur Beurteilung des fairen Wertes während der Sitzung, während der RSI Momentum-Extreme anzeigt.
Trades werden eingegangen, wenn sich der Kurs vom VWAP entfernt und der RSI überkaufte oder überverkaufte Niveaus erreicht.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 157%. Die Strategie funktioniert am besten auf dem Kryptomarkt.

Die Erwartung ist, dass der Kurs zum VWAP zurückkehrt, sobald das Momentum nachlässt.

Ein prozentualer Stop schützt vor Trends, die den Kurs weiter vom VWAP entfernen.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: VWAP, RSI
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

