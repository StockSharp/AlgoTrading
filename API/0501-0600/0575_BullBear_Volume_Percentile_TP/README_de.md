# BullBear Volumen Perzentil TP-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet Bull/Bear Power, normiert durch einen Z-Score.
Long-Positionen werden eröffnet, wenn der Z-Score über den Schwellenwert steigt,
während Short-Positionen eröffnet werden, wenn er unter den negativen Schwellenwert fällt.
Take-Profit-Niveaus basieren auf ATR-Multiplikatoren, angepasst durch Volumen- und Preisperzentile.

## Details

- **Einstiegskriterien:**
  - **Long**: Z-Score kreuzt `ZThreshold` nach oben.
  - **Short**: Z-Score kreuzt `-ZThreshold` nach unten.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Z-Score kreuzt zurück durch null oder Take-Profit-Niveaus werden erreicht.
- **Stops**: Take-Profit über ATR-Multiplikatoren.
- **Standardwerte:**
  - EMA-Länge 21, Z-Score-Länge 252, Schwellenwert 1.618.
  - ATR-Periode 20, Multiplikatoren 1.618 / 2.382 / 3.618.
  - Volumen-MA-Periode 100, Perzentil-Periode 100.
- **Filter:**
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: EMA, ATR
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
