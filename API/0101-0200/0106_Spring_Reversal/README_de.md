# Spring Reversal Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Spring Reversal ist ein Wyckoff-Konzept, bei dem der Preis kurz unter den Support bricht und dann wieder darüber zurückspringt.
Dieser Ausschüttler fängt späte Verkäufer in der Falle und markiert oft den Beginn eines Aufwärtstrends.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 55%. Die Strategie funktioniert am besten am Aktienmarkt.

Die Strategie kauft, sobald der Preis das gebrochene Niveau zurückerobert, in Erwartung schneller Short-Eindeckungen und neuer Nachfrage.

Ein Stop knapp unter dem Spring-Tief begrenzt den Nachteil, und die Position wird geschlossen, wenn die Fortsetzung ausbleibt.

## Details

- **Einstiegskriterien**: Indikatorsignal
- **Long/Short**: Beide
- **Ausstiegskriterien**: Stop-Loss oder entgegengesetztes Signal
- **Stops**: Ja, prozentbasiert
- **Standardwerte**:
  - `CandleType` = 15 minute
  - `StopLoss` = 2%
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Wyckoff
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

