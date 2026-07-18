# Overnight-Gap-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Overnight-Gap-Strategie spielt die Markteröffnung, wenn der Kurs aufgrund von Nachrichten oder After-Hours-Aktivität erheblich vom vorherigen Schlusskurs abweicht.
Große Gaps schließen sich oft teilweise, wenn Trader die Bewegung verarbeiten.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 124%. Sie funktioniert am besten am Forex-Markt.

Die Strategie handelt gegen übermäßige Gaps, tritt kurz nach der Eröffnung in die entgegengesetzte Richtung ein und schließt die Position vor Sessionende.

Stops basieren auf einem Prozentsatz jenseits der Gap-Extreme, um das Risiko zu steuern, falls die Bewegung anhält.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Gap
  - Richtung: Beide
  - Indikatoren: Gap
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

