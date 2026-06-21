# Heiken Ashi Ohne Docht-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt gegen Heiken Ashi-Kerzen, die ohne Dochte erscheinen. Eine bullische Heiken Ashi-Kerze, deren Körper größer als die vorherige ist und keinen unteren Schatten aufweist, löst einen Short-Einstieg aus. Eine bärische Kerze mit längerem Körper und ohne oberen Schatten öffnet eine Long-Position. Positionen werden geschlossen, wenn sich eine entgegengesetzte Kerze ohne den entsprechenden Docht bildet.

## Details

- **Einstiegskriterien**: bullische HA ohne unteren Docht und Körper größer als vorherige für Shorts; bärische HA ohne oberen Docht und Körper größer als vorherige für Longs
- **Long/Short**: Long & Short
- **Ausstiegskriterien**: entgegengesetzt gefärbte HA-Kerze ohne Docht
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 15-Minuten-Kerzen
- **Filter**:
  - Kategorie: Muster
  - Richtung: Umkehr
  - Indikatoren: Heikin-Ashi
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
