# UltraFATL-Schwellenwert-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den UltraFATL-Oszillator, um Verschiebungen in der Trendstärke zu erkennen. Der Indikator gibt diskrete Ebenen von 0 bis 8 aus. Eine Long-Position wird eröffnet, wenn der vorherige Wert über Ebene 4 liegt und der aktuelle Wert unter 5 fällt, während er positiv bleibt. Eine Short-Position wird eröffnet, wenn der vorherige Wert unter 5, aber über null liegt und der aktuelle Wert über 4 steigt. Der Algorithmus arbeitet standardmäßig mit 4-Stunden-Kerzen, der Zeitrahmen kann jedoch angepasst werden.

Der Ansatz erwartet eine Trendfortsetzung nach einem Rückzug von extremen UltraFATL-Werten. Positionen werden umgekehrt, wenn die entgegengesetzte Bedingung erscheint.

## Details

- **Einstiegskriterien**:
  - **Long**: `UltraFATL(prev) > 4` und `UltraFATL(curr) < 5` und `UltraFATL(curr) != 0`.
  - **Short**: `UltraFATL(prev) < 5` und `UltraFATL(prev) != 0` und `UltraFATL(curr) > 4`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**: Das entgegengesetzte Signal kehrt die Position um.
- **Stops**: Standardmäßig nicht verwendet.
- **Standardwerte**:
  - `Candle Type` = 4-Stunden-Kerzen.
  - `Length` = 3.
  - `Signal Bar` = 1 (vorherige Kerze für Signale verwenden).
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Einzeln (UltraFATL)
  - Stops: Nein
  - Komplexität: Moderat
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Moderat
