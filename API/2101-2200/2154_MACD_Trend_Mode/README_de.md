# MACD-Trendmodus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den MACD-Indikator mit drei wählbaren Trenderkennungsmodi handelt: Histogramm-Steigung, Cloud-Crossover oder Nulllinie-Kreuzung.

## Details

- **Einstiegskriterien**:
  - *Histogramm*: Histogramm fiel und dreht nach oben für Longs; stieg und dreht nach unten für Shorts.
  - *Cloud*: MACD-Linie war zuvor über der Signallinie und kreuzt darunter für Long; entgegengesetzter Kreuz für Short.
  - *Null*: Histogramm kreuzt die Nulllinie in entgegengesetzter Richtung.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetzte Bedingungen schließen Positionen.
- **Stops**: Nein.
- **Standardwerte**:
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `TrendMode` = TrendMode.Cloud
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: MACD
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: 4h
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Ja (Histogramm)
  - Risikolevel: Mittel
