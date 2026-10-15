# Bitcoin Intraday-Saisonalitätsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Bitcoin während vordefinierter starker Intraday-Stunden long geht.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 45%. Sie funktioniert am besten auf dem Kryptomarkt.

Das System beobachtet Stundenkerzen. Während ausgewählter UTC-Stunden hält es eine Long-Position, die auf den Portfoliowert dimensioniert ist. Außerhalb dieser Stunden wird auf Cash gewechselt. Aufträge unterhalb eines Mindest-USD-Wertes werden übersprungen.

## Details

- **Einstiegskriterien**: BTC long während der angegebenen UTC-Stunden halten.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Ausstieg außerhalb der angegebenen Stunden.
- **Stops**: Nein.
- **Standardwerte**:
  - `HoursLong` = [0, 1, 2, 3]
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Saisonalität
  - Richtung: Nur Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1h)
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
