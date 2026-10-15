# Zeit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie zur Veranschaulichung von Timing-Dienstprogrammen. Kauft, wenn das Hoch den Eröffnungskurs um eine bestimmte Anzahl von Ticks für eine angegebene Dauer überschreitet.

## Details

- **Einstiegskriterien**: Hoch minus Eröffnung bleibt für die angegebenen Sekunden über dem Schwellenwert.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Bedingung schlägt fehl.
- **Stops**: Nein.
- **Standardwerte**:
  - `TicksFromOpen` = 0
  - `SecondsCondition` = 20
  - `ResetOnNewBar` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Nur Long
  - Indikatoren: Preis
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
