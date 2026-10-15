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
  - `StopLossPercent` = 2
  - `MinGapPercent` = 0.01
    Der Markt handelt rund um die Uhr, daher ist die Sitzung der UTC-Tag. Die Lücke ist der Abstand vom letzten Schlusskurs des Vortags zur ersten Eröffnung des Tages. Eine Lücke von mindestens MinGapPercent wird zum Schluss der ersten Kerze gegengehandelt; der Stop liegt StopLossPercent jenseits des Hochs (Short) oder Tiefs (Long) dieser Kerze und wird auf Kerzenschlüssen geprüft, und die Position schließt mit der letzten Kerze des Tages.
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

