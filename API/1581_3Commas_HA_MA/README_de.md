# 3Commas HA & MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet Heikin Ashi-Kerzen und ein Paar exponentieller gleitender Durchschnitte. Ein Long-Trade tritt auf, wenn einer bearishen HA-Kerze eine bullishe folgt, während der schnelle MA über dem langsamen MA liegt. Shorts folgen der umgekehrten Konstellation. Positionen werden geschlossen, wenn der Preis den langsamen MA kreuzt oder den Swing-Stop erreicht.

## Details
- **Einstiegskriterien**: Heikin Ashi-Umkehr mit MA-Bestätigung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt den langsamen MA oder Stop.
- **Stops**: Swing-Hoch/-Tief.
- **Standardwerte**:
  - `MaFast` = 9
  - `MaSlow` = 18
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Heikin Ashi, EMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
