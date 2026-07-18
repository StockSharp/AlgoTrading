# Strategie Larry Connors RSI 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Mean-Reversion-Strategie basierend auf den RSI-Regeln von Larry Connors.

Die Strategie kauft, wenn der Preis über der 200-Perioden-SMA liegt und der 2-Perioden-RSI drei Tage in Folge von oberhalb eines Auslöseniveaus in den überverkauften Bereich gefallen ist. Positionen werden geschlossen, wenn der RSI über das überkaufte Niveau steigt.

## Details

- **Einstiegskriterien**: Schlusskurs über SMA und 2-Perioden-RSI fällt drei Tage vom Auslöser in den überverkauften Bereich.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: RSI über dem überkauften Niveau.
- **Stops**: Nein.
- **Standardwerte**:
  - `RsiPeriod` = 2
  - `SmaPeriod` = 200
  - `DropTrigger` = 60m
  - `OversoldLevel` = 10m
  - `OverboughtLevel` = 70m
  - `CandleType` = TimeSpan.FromDays(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: RSI, SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
