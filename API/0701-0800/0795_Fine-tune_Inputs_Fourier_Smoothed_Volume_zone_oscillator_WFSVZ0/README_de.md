# Fourier-geglätteter Volumenzonenoszillator WFSVZ0-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie mit einem Fourier-geglätteten Volume Zone Oscillator. Geht Long, wenn der Oszillator über einen Schwellenwert steigt, und Short, wenn er unter den negativen Schwellenwert fällt. Optional werden offene Positionen geschlossen, wenn kein Signal vorhanden ist.

## Details

- **Einstiegskriterien**: Oszillator steigt über den Schwellenwert / fällt unter den negativen Schwellenwert.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder optionales Schließen aller Positionen.
- **Stops**: Keine.
- **Standardwerte**:
  - `VzoLength` = 2
  - `SmoothLength` = 2
  - `Threshold` = 0m
  - `CloseAllPositions` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Volumen
  - Richtung: Beide
  - Indikatoren: Volume Zone Oscillator
  - Stops: Keine
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
