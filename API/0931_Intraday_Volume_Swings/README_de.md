# Intraday-Volumen-Swings-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt, wenn der Kurs in volumenbasierte Swing-Regionen des aktuellen oder vorherigen Tages eintritt.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Kurs dringt in die obere Swing-Region vor.
  - **Short**: Der Kurs dringt in die untere Swing-Region vor.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Entgegengesetztes Signal.
- **Stops**: Nein.
- **Standardwerte**:
  - `RegionMustClose` = true
  - `CandleType` = 1 minute
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Volumen
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
