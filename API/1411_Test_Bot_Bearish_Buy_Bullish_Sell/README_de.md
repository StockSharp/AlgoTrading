# Test-Bot: Bärisch Kaufen / Bullisch Verkaufen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Geht bei der ersten bärischen Kerze Long und schließt bei der ersten bullischen Kerze.

## Details

- **Einstiegskriterien**: Erste bärische Kerze bei flacher Position.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Erste bullische Kerze.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
