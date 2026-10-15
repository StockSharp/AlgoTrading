# Strategie CP Strat ORB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche aus der New Yorker Eröffnungsrange (9:30-9:45) mit einem Retest. Sie geht long, nachdem der Kurs über das Range-Hoch ausbricht und wieder darüber schließt, und geht short, nachdem der Kurs unter das Range-Tief fällt und wieder darunter schließt. Ausstiege verwenden feste Stop-Loss- und Take-Profit-Niveaus.

## Details

- **Einstiegskriterien**: Ausbruch aus der NY-Eröffnungsrange, gefolgt von einem Retest und Schlusskurs jenseits der Range-Grenze.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Fester Take-Profit oder Stop-Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `MinRangePoints` = 60m
  - `StopPoints` = 20m
  - `TakePoints` = 60m
  - `MaxTradesPerSession` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
