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
  - `StopLossPercent` = 2
  - `VolumePeriod` = 20
    Der Markt handelt rund um die Uhr, daher ist die Sitzung der UTC-Tag. Die erste Kerze des Tages ist der Eröffnungsimpuls, wenn ihr Volumen den Durchschnitt der vorherigen VolumePeriod Kerzen übersteigt; die Strategie folgt ihrer Richtung zum Schlusskurs. Die erste Kerze, die gegen die Position schließt, beendet sie, und der Prozent-Stop wird nachgezogen.
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

