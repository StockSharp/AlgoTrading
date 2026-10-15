# Gann Laplace Geglätteter Hybrid-VSA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen Gann-artigen Trendfilter mit Laplace-geglätteter Volumen-Spread-Analyse (VSA). Der VSA-Wert wird als Preisstreuung geteilt durch die Kerzenlänge und multipliziert mit dem Volumen berechnet, dann mit einer EMA geglättet. Trades werden eröffnet, wenn der geglättete VSA mit dem Preis relativ zum Trend-Gleitenden Durchschnitt übereinstimmt.

## Details

- **Einstiegskriterien**:
  - **Long**: geglätteter VSA > 0 und Schlusskurs > Trend-MA.
  - **Short**: geglätteter VSA < 0 und Schlusskurs < Trend-MA.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - **Long**: geglätteter VSA wird negativ.
  - **Short**: geglätteter VSA wird positiv.
- **Stops**: Verwendet StartProtection.
- **Standardwerte**:
  - `Trend Period` = 20
  - `VSA Smoothing` = 14
  - `Candle Type` = 15m
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: MA, Volumen
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
