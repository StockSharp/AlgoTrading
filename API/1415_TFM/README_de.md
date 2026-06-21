# TFM
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Ausbruch-Strategie mit Zeitrahmen-Multiplikator. Verwendet einen höheren Zeitrahmen, der durch Multiplikation des Basiszeitrahmen gebildet wird. Long, wenn der Preis über das vorherige Hoch ausbricht, und optional Short oder Ausstieg, wenn der Preis unter das vorherige Tief fällt.

## Details
- **Einstiegskriterien**: Preis kreuzt Niveaus des multiplizierten Zeitrahmens.
- **Long/Short**: Long mit optionalem Short.
- **Ausstiegskriterien**: Kreuzung des gegenüberliegenden Niveaus oder optionale Umkehr.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleTime` = TimeSpan.FromMinutes(1)
  - `Multiplier` = 2
  - `AllowShort` = false
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide (wenn Shorts aktiviert)
  - Indikatoren: High/Low
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
