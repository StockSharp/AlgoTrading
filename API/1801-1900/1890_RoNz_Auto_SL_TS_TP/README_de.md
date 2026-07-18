# RoNz Auto SL TS TP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Positionen bei EMA-Kreuzungen eröffnet und Stop-Loss- und Take-Profit-Niveaus automatisch verwaltet.  
Nach dem Einstieg werden initialer Stop und Ziel gesetzt, anschließend wird optional der Gewinn gesichert und ein Trailing-Stop aktiviert.

## Details

- **Einstiegskriterien**:
  - Long: `EMA10 < EMA20 && EMA10 > EMA100`
  - Short: `EMA10 > EMA20 && EMA10 < EMA100`
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss, Take-Profit, Gewinnsperre oder Trailing-Stop
- **Stops**: Ja
- **Standardwerte**:
  - `TakeProfit` = 500
  - `StopLoss` = 250
  - `LockProfitAfter` = 100
  - `ProfitLock` = 60
  - `TrailingStop` = 50
  - `TrailingStep` = 10
- **Filter**:
  - Kategorie: Risikomanagement
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: SL/TP/Trailing
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
