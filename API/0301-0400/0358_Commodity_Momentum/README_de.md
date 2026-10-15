# Rohstoff-Momentum-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Commodity Momentum**-Strategie geht long bei Rohstoffen mit dem stärksten 12-Monats-Momentum (unter Auslassung des letzten Monats).
Positionen werden am ersten Handelstag jedes Monats neu gewichtet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 10%. Sie funktioniert am besten auf diversifizierten Rohstoffmärkten.

Positionen werden monatlich angepasst; es werden keine Intraday-Signale verwendet.

## Details
- **Einstiegskriterien**: Die `TopN` Rohstoffe nach 12-Monats-Momentum (ohne letzten Monat) kaufen.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Neugewichtung am nächsten geplanten Datum.
- **Stops**: Keine explizite Stop-Logik.
- **Standardwerte**:
  - `TopN = 5`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Nur Long
  - Indikatoren: Price
  - Stops: Nein
  - Komplexität: Mittel
  - Zeitrahmen: Täglich
  - Saisonalität: Ja
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
