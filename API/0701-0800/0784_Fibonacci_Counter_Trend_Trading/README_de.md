# Fibonacci Gegentrend-Handelsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet einen volumengewichteten gleitenden Durchschnitt (VWMA) und die Standardabweichung, um Fibonacci-Bänder zu konstruieren. Sie geht long, wenn der Preis unter das ausgewählte untere Band fällt, und short, wenn der Preis über das obere Band steigt. Optional werden Positionen geschlossen, wenn der Preis die VWMA-Basis kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Der Schlusskurs kreuzt nach unten unter das gewählte untere Band.
  - **Short**: Der Schlusskurs kreuzt nach oben über das gewählte obere Band.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - **Basis**: Optionaler Ausstieg, wenn der Preis die VWMA kreuzt.
  - **Umkehr**: Das gegenüberliegende Bandsignal kehrt die Position um.
- **Stops**: Keine.
- **Indikatoren**: VolumeWeightedMovingAverage, StandardDeviation.
