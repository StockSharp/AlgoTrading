# Autostop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Hilfsstrategie, die automatisch Take-Profit und Stop-Loss für offene Positionen setzt.
Sie generiert keine Handelssignale. Positionen, die extern geöffnet wurden, werden mit festen Abständen geschützt.

## Details

- **Einstiegskriterien**: Keine, Orders werden außerhalb der Strategie verwaltet.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Nur Schutzaufträge.
- **Stops**: Verwendet StartProtection zum Platzieren fester Take-Profit- und Stop-Loss-Aufträge.
- **Standardwerte**:
  - `MonitorTakeProfit` = true
  - `MonitorStopLoss` = true
  - `TakeProfitTicks` = 30
  - `StopLossTicks` = 30
- **Filter**:
  - Kategorie: Risikomanagement
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Fest
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
