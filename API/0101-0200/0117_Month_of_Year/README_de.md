# Strategie des Monats-im-Jahres-Effekts
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der Monats-im-Jahres-Effekt erfasst Leistungsunterschiede, die in verschiedenen Monaten beobachtet werden.
Beispielsweise steigen Aktien häufig im November und Dezember, können jedoch im September schwach sein.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 88%. Es funktioniert am besten auf dem Aktienmarkt.

Das System geht zu Beginn jedes Monats long oder short basierend auf diesen historischen Durchschnittswerten und verlässt die Position am Monatsende.

Stops werden eingesetzt, um das Kapital zu schützen, falls das übliche saisonale Verhalten ausbleibt.

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

