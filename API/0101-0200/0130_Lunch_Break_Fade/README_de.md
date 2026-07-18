# Mittagspausen-Fade-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Mittagspausen-Fade-Strategie zielt auf Umkehrungen ab, die sich während der ruhigen Mittagszeit entwickeln.
Nach der Morgensitzung pausieren oder korrigieren Trends häufig, wenn das Volumen zur Mittagszeit abnimmt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 127%. Sie funktioniert am besten am Aktienmarkt.

Die Strategie handelt gegen die Morgenbewegung gegen Mittag, tritt entgegen der vorherrschenden Richtung ein und schließt die Position, bevor das Volumen zurückkehrt.

Ein prozentualer Stop steuert das Risiko, falls der Trend wieder aufgenommen wird statt abzuflauen.

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

