# Adaptives Marktniveau
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die auf Basis des Adaptive Market Level (AML)-Indikators handelt. Der Indikator passt sich der aktuellen Volatilität an und zeichnet ein dynamisches Preisniveau. Eine Long-Position wird eröffnet, wenn die AML-Linie nach oben dreht, und eine Short-Position, wenn sie nach unten dreht. Entgegengesetzte Positionen werden bei einem Farbwechsel oder beim Auslösen von Stop-Loss/Take-Profit geschlossen.

Das System folgt mittelfristigen Trends und arbeitet standardmäßig auf höheren Zeitrahmen.

## Details

- **Einstiegskriterien**: AML-Linie ändert die Richtung nach oben für Longs und nach unten für Shorts.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: AML-Richtungsänderung oder Stop/Ziel.
- **Stops**: Ja.
- **Standardwerte**:
  - `Fractal` = 6
  - `Lag` = 7
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Adaptive Market Level
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: H4
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
