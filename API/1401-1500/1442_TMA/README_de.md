# TMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die TMA-Strategie verwendet mehrere geglättete gleitende Durchschnitte und Kerzenmuster, um in Richtung des 200-Perioden-Trends zu handeln. Sie kombiniert 3-Linien-Strike- und Engulfing-Signale mit einem Sitzungsfilter.

## Details

- **Einstiegskriterien**: bullisches Engulfing oder 3-Linien-Strike im Aufwärtstrend / bärisches Engulfing oder 3-Linien-Strike im Abwärtstrend mit EMA(2) über/unter SMA(200) und optionalem Sitzungsfilter
- **Long/Short**: Beide
- **Ausstiegskriterien**: EMA(2) kreuzt SMA(200)
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 5-Minuten-Kerzen
  - `FastLength` = 21
  - `MidLength` = 50
  - `Mid2Length` = 100
  - `SlowLength` = 200
  - `UseSession` = false
  - `SessionStart` = 08:30
  - `SessionEnd` = 12:00
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: SMA, EMA
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
