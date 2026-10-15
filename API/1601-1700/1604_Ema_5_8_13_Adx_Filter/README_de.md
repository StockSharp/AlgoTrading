# EMA 5-8-13 mit ADX-Filter Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt EMA-Kreuzungen bei 5 und 8 Perioden und verwendet eine 13-Perioden-EMA für Ausstiege. Ein optionaler ADX-Filter bestätigt die Trendstärke. Long-Positionen entstehen, wenn EMA5 EMA8 von unten kreuzt und ADX den Schwellenwert übersteigt. Short-Positionen werden beim entgegengesetzten Signal eröffnet.

## Details

- **Einstiegskriterien**:
  - **Long**: EMA5 kreuzt EMA8 von unten und ADX > Schwellenwert.
  - **Short**: EMA5 kreuzt EMA8 von oben und ADX > Schwellenwert.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - **Long**: Schlusskurs < EMA13
  - **Short**: Schlusskurs > EMA13
- **Stops**: Nein.
- **Standardwerte**:
  - `ADX threshold` = 20
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig
