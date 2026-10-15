# Goldene-Transformation-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert den Rate-of-Change-Indikator mit einem dreifachen Hull-basierten TRIX, einem Hull-MA-Filter und einem geglätteten Fisher Transform. Long-Trades werden geöffnet, wenn ROC über TRIX kreuzt, während TRIX unter null liegt und der Eröffnungspreis über dem Hull MA liegt. Short-Trades erfolgen beim gegenteiligen Signal. Positionen werden bei entgegengesetzten Kreuzungen oder wenn der geglättete Fisher die Schwellenwerte überschreitet und sich umkehrt, geschlossen.

## Details

- **Einstiegskriterien**:
  - **Long**: `ROC crosses above TRIX` && `TRIX < 0` && `Open > Hull MA`
  - **Short**: `ROC crosses below TRIX` && `TRIX > 0` && `Open < Hull MA`
- **Long/Short**: Long und Short
- **Ausstiegskriterien**:
  - Long: `ROC crosses below TRIX` ODER (`Fisher HMA > 1.5` && `Fisher HMA crosses below previous Fisher`)
  - Short: `ROC crosses above TRIX` ODER (`Fisher HMA < -1.5` && `Fisher HMA crosses above previous Fisher`)
- **Stops**: Nein
- **Standardwerte**:
  - `ROC Length` = 50
  - `Hull TRIX Length` = 90
  - `Hull Entry Length` = 65
  - `Fisher Length` = 50
  - `Fisher Smooth Length` = 5
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: ROC, Hull MA, Fisher Transform
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
