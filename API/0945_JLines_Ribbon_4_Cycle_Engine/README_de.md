# J-Lines Ribbon 4-Zyklus-Motor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die J-Lines Ribbon 4-Cycle Engine Strategie klassifiziert den Markt in CHOP-, LONG- und SHORT-Zyklen mithilfe eines EMA-Bandes und des Average Directional Index. Einstiege erfolgen bei neuen Zykluserkennungen und Rebounds von wichtigen EMAs, während Ausstiege bei entgegengesetzten Kreuzungen oder Swing-Breaks ausgelöst werden.

## Details

- **Einstiegskriterien**:
  - **Long**: Neuer LONG-Zyklus oder Rebound über EMA72/EMA126, während EMA72 über EMA89 liegt.
  - **Short**: Neuer SHORT-Zyklus oder Rebound unter EMA72/EMA126, während EMA72 unter EMA89 liegt.
- **Stops**: Letztes Swing-Hoch/-Tief.
- **Standardwerte**:
  - `DmiLength` = 8
  - `AdxFloor` = 12
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA, ADX
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
