# Pivot Point Umkehrstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tägliche Pivot Points und ihre Unterstützungs- und Widerstandsniveaus fungieren häufig als Wendepunkte für die Intraday-Kursentwicklung. Diese Strategie berechnet die klassischen Floor-Trader-Pivots aus dem Hoch, Tief und Schlusskurs des Vortages und sucht dann nach Kerzen, die von S1 oder R1 abprallen.

Tests zeigen eine durchschnittliche Jahresrendite von etwa 127%. Die Strategie eignet sich am besten für den Aktienmarkt.

Wenn der Kurs das Unterstützungsniveau S1 erreicht und eine bullische Kerze bildet, wird eine Long-Position eröffnet. Wenn der Kurs das Widerstandsniveau R1 testet und eine bearische Kerze druckt, wird ein Short eröffnet. Trades werden beim Erreichen des zentralen Pivots oder beim Auslösen des Schutzstops beendet.

Die Methode wird zu Beginn jedes Handelstages mit neuen Pivot-Berechnungen zurückgesetzt, was sie für Handelssitzungen mit klaren Intraday-Spannen gut geeignet macht.

## Details

- **Einstiegskriterien**: Bullische Kerze nahe S1 oder bearische Kerze nahe R1.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Kurs kreuzt den zentralen Pivot oder Stop-Loss.
- **Stops**: Ja, prozentbasiert.
- **Standardwerte**:
  - `CandleType` = 5 minute
  - `StopLossPercent` = 2
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Pivot Points
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

