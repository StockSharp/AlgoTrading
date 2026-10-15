# Bedo Osaimi Istr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Eine einfache Trendfolge-Strategie, die gleitende Durchschnitte der Schluss- und Eröffnungspreise vergleicht. Eine Long-Position wird eröffnet, wenn der gleitende Durchschnitt des Schlusskurses den gleitenden Durchschnitt des Eröffnungskurses nach oben kreuzt. Die Position wird umgekehrt, wenn der entgegengesetzte Kreuzung eintritt.

## Details

- **Einstiegskriterien**:
  - Schluss-MA kreuzt über Eröffnungs-MA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Schluss-MA kreuzt unter Eröffnungs-MA (für Long-Ausstieg oder Short-Einstieg).
- **Stops**: Keine.
- **Standardwerte**:
  - `MaLength` = 20
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: SMA auf Schluss- und Eröffnungskurs
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
