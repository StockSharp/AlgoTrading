# Strategie Erster Handelstag des Monats
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Viele Märkte zeigen am ersten Handelstag des Monats eine bullishe Tendenz, da neues Kapital in Fonds fließt.
Trader versuchen, diesem Effekt zuvorzukommen, indem sie am letzten Kurs des Vormonats oder früh in der Session kaufen.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 97%. Es funktioniert am besten auf dem Kryptomarkt.

Die Strategie geht zu Beginn des Monats long und verlässt die Position vor Beginn des zweiten Tages, um den typischen Kaufzufluss zu erfassen.

Ein kleiner Stop schützt vor negativen Überraschungen, falls die erwartete Stärke ausbleibt.

## Details

- **Einstiegskriterien**: Kalendereffekt-Auslöser
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Beide
  - Indikatoren: Saisonalität
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

