# Open Drive Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Open Drive bezeichnet eine starke gerichtete Bewegung direkt nach der Markteröffnung, oft nach einem Nachrichtenkatalysator über Nacht.
Trader suchen nach hohem Volumen und nachhaltigem Momentum in den ersten Minuten.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 118%. Sie funktioniert am besten am Aktienmarkt.

Die Strategie schließt sich diesem Momentum an, tritt long oder short innerhalb der Eröffnungsspanne ein und zieht den Stop nach, während der Kurs sich ausdehnt.

Positionen werden schnell geschlossen, wenn der Antrieb nachlässt, um Verluste bei unruhigen Eröffnungen klein zu halten.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Intraday
  - Richtung: Beide
  - Indikatoren: Price Action
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

