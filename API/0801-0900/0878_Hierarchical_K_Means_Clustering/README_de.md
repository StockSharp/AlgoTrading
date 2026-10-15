# Hierarchisches K-Means-Clustering-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wendet Volatilitätsclustering auf ein SuperTrend-System an. ATR-Werte werden in drei Cluster gruppiert, um das Marktregime zu bestimmen, während die SuperTrend-Richtung Einstiege auslöst. Ein optionaler gleitender Durchschnitt und ADX-Filter bestätigen die Trendstärke. Positionen können vorzeitig geschlossen werden, wenn das Bullen-/Bären-Volumenverhältnis sich dem Gleichgewicht nähert.

## Details

- **Einstiegskriterien**:
  - **Long**: SuperTrend wird bullisch && Cluster-Trend > 0 && Filter bestanden.
  - **Short**: SuperTrend wird bärisch && Cluster-Trend < 0 && Filter bestanden.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Volumengleichgewicht oder entgegengesetztes Signal.
- **Stops**: Nur volumenbasiert.
- **Standardwerte**:
  - `ATR Length` = 11.
  - `SuperTrend Factor` = 3.
  - `Training Data Length` = 200.
  - `Moving Average Length` = 50.
  - `Trend Strength Period` = 14.
  - `Trend Strength Threshold` = 20.
  - `Volume Ratio Threshold` = 0.9.
  - `Delay Bars` = 4.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Komplex
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
