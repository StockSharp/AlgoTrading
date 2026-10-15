# Doppelhandel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Pairs-Trading-Strategie, die entgegengesetzte Positionen auf zwei korrelierten Instrumenten eröffnet und sie schließt, wenn der kombinierte Gewinn ein Ziel erreicht.

## Details

- **Einstiegskriterien**: gleichzeitig erstes und zweites Instrument in entgegengesetzten Richtungen eröffnen
- **Long/Short**: Long & Short
- **Ausstiegskriterien**: kombinierter Gewinn >= ProfitTarget
- **Stops**: Nein
- **Standardwerte**:
  - `Volume1` = 1
  - `Volume2` = 1.3
  - `ProfitTarget` = 20
  - `SecondSecurity` = erforderlich
- **Filter**:
  - Kategorie: Pairs Trading
  - Richtung: Abgesichert
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
