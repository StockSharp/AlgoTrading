# Gaussian Detrended Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Gaussian Detrended Reversion ist eine Mean-Reversion-Strategie, die einen detrendierten Preisoszillator verwendet, der mit einem Arnaud Legoux Moving Average (ALMA) geglättet wird. Long-Positionen werden eröffnet, wenn der geglättete Oszillator seine verzögerte Version von unten kreuzt und dabei unter null liegt; Shorts werden bei Abwärtskreuzungen über null eröffnet. Positionen werden bei entgegengesetzten Kreuzungen oder beim Kreuzen der Nulllinie geschlossen.

## Details
- **Daten**: Kurskerzen.
- **Einstiegskriterien**:
  - **Long**: ALMA-geglätteter DPO kreuzt seine Verzögerungslinie von unten und liegt unter null.
  - **Short**: ALMA-geglätteter DPO kreuzt seine Verzögerungslinie von oben und liegt über null.
- **Ausstiegskriterien**: Entgegengesetzter Verzögerungskreuzung oder Nulllinienkreuzung.
- **Stops**: Keine.
- **Standardwerte**:
  - `PriceLength` = 52
  - `SmoothingLength` = 52
  - `LagLength` = 26
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long/Short
  - Indikatoren: EMA, ALMA
  - Komplexität: Niedrig
  - Risikolevel: Mittel
