# SPY TLT-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kauft das Hauptwertpapier, wenn der TLT-Preis über seine SMA kreuzt, und steigt aus, wenn TLT unter der SMA schließt. Der Handel ist nur innerhalb des angegebenen Zeitfensters erlaubt.

## Details

- **Einstiegskriterien**:
  - **Long**: TLT schließt innerhalb des Zeitfensters über seiner SMA.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - TLT schließt unter seiner SMA.
- **Stops**: Keine.
- **Standardwerte**:
  - `Start Time` = 2014-01-01
  - `End Time` = 2099-01-01
  - `TLT Symbol` = TLT
  - `SMA Length` = 20
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Nur Long
  - Indikatoren: SMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
