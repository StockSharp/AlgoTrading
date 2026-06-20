# ADX-Volumen-Multiplikator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die ADX-Volumen-Multiplikator-Strategie kombiniert die Trendstärke des Average Directional Index mit einem Volumenanstiegsfilter. Sie tritt in Trades nur ein, wenn der ADX einen Schwellenwert überschreitet, die dominante Richtungslinie in die Trendrichtung zeigt und das aktuelle Volumen einen gleitenden Durchschnitt multipliziert mit einem benutzerdefinierten Faktor überschreitet.

## Details

- **Einstiegskriterien**:
  - ADX über Schwellenwert und DI+ > DI- mit Volumen größer als SMA(Volumen) * Multiplikator → Long.
  - ADX über Schwellenwert und DI- > DI+ mit Volumen größer als SMA(Volumen) * Multiplikator → Short.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Ein umgekehrtes Signal löst eine Positionsumkehr aus.
- **Stops**: Keine.
- **Standardwerte**:
  - `AdxPeriod` = 21
  - `AdxThreshold` = 26
  - `VolumeMultiplier` = 1.8
  - `VolumePeriod` = 20
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ADX, Volume SMA
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
