# Montags-Schwäche-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Montags-Schwäche beschreibt, dass Aktien nach dem Wochenende häufig tiefer eröffnen, da Trader Nachrichten verarbeiten und Positionen umschichten.
Kurzfristiger Baissdruck kann zu Wochenbeginn auftreten, bevor sich die Märkte stabilisieren.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 106%. Sie funktioniert am besten am Aktienmarkt.

Die Strategie geht bei der Montagseröffnung short und schließt die Position zum Börsenschluss, um von dieser anfänglichen Schwäche zu profitieren.

Stops werden eng gehalten, um Verluste zu vermeiden, falls der Markt die Tendenz bricht und steigt.

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

