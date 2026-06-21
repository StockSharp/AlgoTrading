# HOD/LOD/PMH/PML/PDH/PDL-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche aus Vormarkt- und Vortageslevels.
Long-Einstiege erfolgen, wenn der Kurs über das Vormarkt- oder Vortageshoch kreuzt.
Short-Einstiege erfolgen, wenn der Kurs unter das Vormarkt- oder Vortagestief kreuzt.
Positionen werden geschlossen, wenn der Kurs das Tageshoch oder -tief erreicht.

## Details

- **Einstiegskriterien**: Kurs kreuzt Vormarkt- oder Vortageslevels
- **Long/Short**: Beide
- **Ausstiegskriterien**: Erreichen des aktuellen Tageshochs oder -tiefs
- **Stops**: Nein
- **Standardwerte**:
  - `CandleType` = 5 Minuten
- **Filter**:
  - Kategorie: Levels
  - Richtung: Beide
  - Indikatoren: Keine
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
