# Bollinger Bands Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft, wenn der Preis unter das untere Bollinger Band schließt, und steigt aus, wenn der Preis über das obere Band schließt.

## Details

- **Einstiegskriterien**:
  - **Long**: Schlusskurs unter der unteren Band.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Schlusskurs über der oberen Band.
- **Stops**: Keine.
- **Standardwerte**:
  - Bollinger Bands Länge 20.
  - Multiplikator 2.
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: Bollinger Bands
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig
