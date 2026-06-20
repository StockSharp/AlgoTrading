# Strategie des Wochentagseffekts
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Der Wochentagseffekt nutzt die Tendenz der Märkte, an bestimmten Wochentagen wiederkehrende Muster zu zeigen.
Einige Indizes zeigen eine konsistente Stärke in der Wochenmitte, während Montag oder Freitag relativ schwach sein können.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 85%. Es funktioniert am besten auf dem Kryptomarkt.

Die Strategie eröffnet Trades basierend auf diesen historischen Tendenzen, kauft oder verkauft zu Beginn der Session und schließt bis zum Schlusskurs.

Ein moderater Stop schützt vor Anomalien und schließt die Position vorzeitig, wenn das Muster an einem bestimmten Tag versagt.

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

