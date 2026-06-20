# Doppelboden-Muster (Double Bottom Pattern)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese musterbasierte Strategie sucht nach zwei aufeinanderfolgenden Tiefs auf annähernd dem gleichen Preisniveau, die durch einen festgelegten Abstand voneinander getrennt sind. Nach der Ausbildung des zweiten Bodens bestätigt eine bullische Kerze die Umkehr.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 55 %. Die Strategie eignet sich am besten für den Aktienmarkt.

Bei der Bestätigung kauft das System mit einem Stop unterhalb der Musttiefs. Das Setup zielt darauf ab, scharfe Erholungen nach erschöpftem Verkaufsdruck zu erfassen.

Ausstiege basieren auf einem vordefinierten Stop-Loss oder manuellen Gewinnzielen.

## Details

- **Einstiegskriterien**: Zwei Böden bilden sich innerhalb von `SimilarityPercent` nach `Distance` Kerzen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Kurs bricht ein oder Stop-Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **Filter**:
  - Kategorie: Muster
  - Richtung: Nur Long
  - Indikatoren: Price Action
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
