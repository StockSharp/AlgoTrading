# Täglicher Ausbruch mit Tages-Schatten-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt tägliche Ausbrüche anhand der letzten zwei abgeschlossenen Tageskerzen. Offene Positionen werden zu Beginn jedes neuen Tages geschlossen.

## Details

- **Einstiegskriterien**:
  - Long: Der vorherige Tag schließt über dem Körperhoch der vorvorigen Kerze und öffnet unter diesem Niveau.
  - Short: Der vorherige Tag schließt unter dem Körpertief der vorvorigen Kerze und öffnet über diesem Niveau.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Position wird zu Beginn eines neuen Tages geschlossen.
- **Stops**: Nein.
- **Standardwerte**:
  - `CandleType` = 1 Day
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Niedrig
  - Zeitrahmen: Täglich
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
