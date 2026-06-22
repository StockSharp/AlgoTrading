# Color Schaff DeMarker Trendzyklen-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Color Schaff DeMarker Trendzyklen-Strategie** verwendet einen benutzerdefinierten Oszillator, der aus schnellen und langsamen DeMarker-Werten abgeleitet wird. Der Indikator wendet zwei stochastische Schritte an, um einen Zykluswert zu erzeugen, der zwischen -100 und +100 oszilliert. Farben werden basierend auf dem Niveau und der Steigung des Oszillators zugewiesen, die dann zur Erzeugung von Handelssignalen verwendet werden.

Die Strategie tritt in Long-Positionen ein, wenn der Oszillator die obere Zone verlässt, und verlässt Short-Positionen. Sie öffnet Short-Positionen, wenn der Oszillator die untere Zone verlässt, und verlässt Long-Positionen. Die Idee besteht darin, auf Momentum-Änderungen bei extremen Niveaus zu reagieren.

## Details

- **Einstiegskriterien**:
  - **Long**: Vorherige Farbe > 5 und aktuelle Farbe < 6.
  - **Short**: Vorherige Farbe < 2 und aktuelle Farbe > 1.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - **Long**: Farbe < 2 bei offener Long-Position.
  - **Short**: Farbe > 5 bei offener Short-Position.
- **Stops**: Kein expliziter Stop-Loss oder Take-Profit.
- **Standardwerte**:
  - `FastDeMarker` = 23
  - `SlowDeMarker` = 50
  - `Cycle` = 10
  - `HighLevel` = 60
  - `LowLevel` = -60
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: DeMarker, Highest, Lowest
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: 4H
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
