# Strategie Neues Intraday-Hoch mit Schwacher Bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Geht Long bei einem neuen `HighestLength`-Bar-Hoch, wenn die Kerze nahe ihrem Tief schließt. Ausstieg, wenn der Kurs über das Hoch der vorherigen Bar schließt.

## Details

- **Einstiegskriterien**:
  - Keine Position, Hoch entspricht dem höchsten Hoch der letzten `HighestLength` Bars und `(close - low)/(high - low) < WeakRatio`.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schlusskurs über dem Hoch der vorherigen Bar.
- **Stops**: Nein.
- **Standardwerte**:
  - `HighestLength` = 10
  - `WeakRatio` = 0.15
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Nur Long
  - Indikatoren: Highest
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
