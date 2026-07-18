# LSMA Schnelle und Einfache Alternative Berechnungsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet eine schnelle Annäherung des Least Squares Moving Average (LSMA), berechnet als `3 × WMA − 2 × SMA`. Eine Long-Position wird eröffnet, wenn der Kurs über die LSMA kreuzt, eine Short-Position, wenn er darunter kreuzt.

## Details

- **Einstiegskriterien**:
  - **Long**: Close kreuzt über LSMA.
  - **Short**: Close kreuzt unter LSMA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegensätzliches Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - Länge 25.
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: WMA, SMA
  - Stops: Nein
  - Komplexität: Einfach
  - Zeitrahmen: Nicht angegeben
