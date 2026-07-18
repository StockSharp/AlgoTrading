# PriceChannel Stop
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf dem Price Channel Stop-Indikator.

Der Indikator berechnet das höchste Hoch und das niedrigste Tief über den angegebenen Zeitraum, um einen Donchian-Kanal zu bilden. Stop-Niveaus werden mithilfe des `Risk`-Faktors innerhalb des Kanals aufgebaut. Wenn der Kurs über dem oberen Stop schließt, wechselt der Trend auf bullisch; schließt er unter dem unteren Stop, wechselt der Trend auf bärisch. Die Strategie eröffnet Positionen bei diesen Umkehrungen und schließt optional entgegengesetzte Positionen.

## Details

- **Einstiegskriterien**: Kurs kreuzt Stop-Niveaus.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `ChannelPeriod` = 5
  - `Risk` = 0.10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Donchian-Kanal
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1h)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
