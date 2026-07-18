# Price Flip-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Price Flip-Strategie spiegelt den Preis um aktuelle Hochs und Tiefs und handelt gleitende Durchschnitt-Crossover, wenn der vorherige Schlusskurs auf der entgegengesetzten Seite dieses invertierten Preises liegt. Ein Trendfilter basierend auf dem langsamen gleitenden Durchschnitt kann angewendet werden.

## Details

- **Einstiegskriterien**:
  - Der vorherige Schlusskurs liegt über dem invertierten Preis.
  - Schneller MA kreuzt den langsamen MA nach oben.
  - Optional: Preis liegt über dem langsamen MA, wenn der Trendfilter aktiviert ist.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Das entgegengesetzte Signal löst eine Umkehr aus.
- **Stops**: Keine.
- **Standardwerte**:
  - `TickerMaxLookback` = 100
  - `TickerMinLookback` = 100
  - `FastMaLength` = 12
  - `SlowMaLength` = 14
  - `UseTrendFilter` = true
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA, Highest/Lowest
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
