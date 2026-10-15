# Heiken Ashi Wellen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Heikin-Ashi-Kerzen mit einem Doppel-Moving-Average-Wellenfilter kombiniert. Das Kreuzen der schnellen SMA (2) über die langsame SMA (30) signalisiert mögliche Wellenänderungen und wird durch die aktuelle Heikin-Ashi-Kerzenrichtung bestätigt.

## Details

- **Einstiegskriterien**:
  - Long: bullische Heikin-Ashi-Kerze und schnelle SMA kreuzt über langsame SMA
  - Short: bärische Heikin-Ashi-Kerze und schnelle SMA kreuzt unter langsame SMA
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Entgegengesetzter Kreuzungspunkt
  - Trailing Stop Loss
- **Stops**: Trailing Stop in Punkten über `StopLoss`
- **Standardwerte**:
  - `FastLength` = 2
  - `SlowLength` = 30
  - `StopLoss` = new Unit(20, UnitTypes.Point)
  - `UseTrailing` = true
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, SMA
  - Stops: Ja
  - Komplexität: Anfänger
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
