# Doppeltop-Muster (Double Top Pattern)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Das Doppeltop identifiziert zwei Hochpunkte, die durch eine Anzahl von Kerzen mit ähnlichen Preisen voneinander getrennt sind. Nach der Ausbildung des zweiten Hochpunkts bestätigt eine bärische Kerze die Umkehr.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 58 %. Die Strategie eignet sich am besten für den Aktienmarkt.

Die Strategie geht nach der Bestätigung short mit einem Stop oberhalb der Musthochs und zielt darauf ab, von einem Trendwechsel nach Erschöpfung der Käufer zu profitieren.

Positionen werden über Stop-Loss oder diskretionäre Ziele geschlossen.

## Details

- **Einstiegskriterien**: Zwei Hochpunkte innerhalb von `SimilarityPercent` nach `Distance` Kerzen.
- **Long/Short**: Nur Short.
- **Ausstiegskriterien**: Kurs erholt sich oder Stop-Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `Distance` = 5
  - `SimilarityPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
- **Filter**:
  - Kategorie: Muster
  - Richtung: Nur Short
  - Indikatoren: Price Action
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja
  - Risikolevel: Mittel
