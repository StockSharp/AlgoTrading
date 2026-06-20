# Strategie mit rückwärtiger Balkenanzahl
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie hält eine Long-Position nur während der jüngsten, vom aktuellen Zeitpunkt rückwärts gezählten Balken. Sie zeigt, wie der Handel auf ein gleitendes historisches Fenster beschränkt werden kann.

## Details

- **Einstiegskriterien**: Kerzenzeit liegt innerhalb der letzten *N* Balken ab der Startzeit.
- **Ausstiegskriterien**: Kerzenzeit fällt aus diesem Fenster heraus.
- **Long/Short**: Nur Long.
- **Stops**: Keine.
- **Standardwerte**:
  - `Bar count` = 50
  - `Candle type` = 1-Minuten-Kerzen
- **Filter**:
  - Kategorie: Zeitbasiert
  - Richtung: Long
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Niedrig
