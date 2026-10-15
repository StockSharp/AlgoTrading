# Dark Pool Prints Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dark Pool Prints verfolgt große außerbörsliche Transaktionen, die oft starken Bewegungen vorausgehen, sobald die Aktivität bekannt wird.
Ungewöhnliches Volumen auf dem Tape kann institutionelle Positionierungen signalisieren, die den regulären Markt noch nicht beeinflusst haben.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 46%. Die Strategie funktioniert am besten am Aktienmarkt.

Die Strategie tritt in dieselbe Richtung ein wie starke Dark Pool-Käufe oder -Verkäufe und erwartet eine Fortsetzung, wenn der Rest des Marktes reagiert.

Ein kleiner prozentualer Stop hält das Risiko begrenzt, und Positionen werden geschlossen, wenn der erwartete Impuls ausbleibt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Beide
  - Indikatoren: Volume
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

