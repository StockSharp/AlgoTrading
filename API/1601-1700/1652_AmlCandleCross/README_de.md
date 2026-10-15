# AML-Kerzen-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt basierend auf dem Adaptive Market Level (AML) Indikator.
Ein Trade wird eröffnet, wenn der AML-Wert innerhalb des aktuellen Kerzenkörpers liegt:
Wenn die Kerze über der Eröffnung schließt und AML dazwischen liegt, wird eine Long-Position
eröffnet. Bei bärischen Kerzen öffnet die entgegengesetzte Bedingung eine Short-Position.
Optional kann die Position umgekehrt werden, wenn das entgegengesetzte Signal erscheint.

## Details

- **Einstiegskriterien**:
  - **Long**: bullische Kerze und `open <= AML <= close`.
  - **Short**: bärische Kerze und `open >= AML >= close`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Position bei entgegengesetztem Signal umgekehrt, wenn aktiviert.
- **Stops**: Keine.
- **Standardwerte**:
  - `Fractal` = 70
  - `Lag` = 18
  - `Shift` = 0
  - `UseOpposite` = true
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln (AML)
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Kurzfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
