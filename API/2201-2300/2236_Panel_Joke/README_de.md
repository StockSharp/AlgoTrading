# Panel Joke-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert das originale MetaTrader-*panel-joke*-System in StockSharp. Sie vergleicht die aktuelle Kerze mit der vorherigen anhand von sieben Preiskennzahlen (Eröffnung, Hoch, Tief, Durchschnitt von Hoch und Tief, Schluss, Durchschnitt von Hoch/Tief/Schluss und gewichteter Durchschnitt von Hoch/Tief/Schluss). Jede gestiegene Kennzahl zählt für ein potentielles Long-Setup; jede gefallene für ein Short-Setup.

Wenn der Parameter `Enable Autopilot` auf `true` gesetzt ist, öffnet oder kehrt die Strategie Positionen automatisch basierend darauf, welche Seite mehr Punkte hat. Keine zusätzlichen Indikatoren oder Stop-Regeln werden verwendet.

## Details

- **Einstiegskriterien**:
  - **Long**: Buy counter > Sell counter.
  - **Short**: Sell counter > Buy counter.
- **Ausstiegskriterien**: Umkehrung bei entgegengesetztem Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `Enable Autopilot` = `true`.
  - `Candle Type` = 5-Minuten-Zeitrahmen.
- **Filter**:
  - Kategorie: Price action
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Hoch

