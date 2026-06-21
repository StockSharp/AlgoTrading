# Dreieck-Ausbruch-Strategie mit TP, SL und EMA-Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Erkennt Dreiecksmuster aus Pivot-Hochs und -Tiefs. Geht bei Ausbruch über das Dreieck long, optional mit Anforderung, dass der Preis über EMA20 und EMA50 liegt, und verwendet prozentbasierte Take-Profit- und Stop-Loss-Niveaus.

## Details

- **Einstiegskriterien**: Schlusskurs über der oberen Dreieckslinie mit optionalem EMA20/EMA50-Filter
- **Long/Short**: Long
- **Ausstiegskriterien**: prozentualer Take-Profit oder Stop-Loss
- **Stops**: Ja
- **Standardwerte**:
  - `PivotLength` = 5
  - `TakeProfitPercent` = 3
  - `StopLossPercent` = 1.5
  - `UseEmaFilter` = true
  - `EmaFast` = 20
  - `EmaSlow` = 50
  - `CandleType` = 1 Stunde
- **Filter**:
  - Kategorie: Muster
  - Richtung: Long
  - Indikatoren: EMA, Pivot
  - Stops: Ja
  - Komplexität: Fortgeschritten
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
