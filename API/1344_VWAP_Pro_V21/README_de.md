# Strategie VWAP Pro V21
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie kombiniert schnelle und langsame EMA mit VWAP und ATR-basiertem Risikomanagement. Ein EMA-Filter auf einem höheren Zeitrahmen (1h, Länge 50) bestätigt den Trend. Trades öffnen sich, wenn der Preis mit dem Trend übereinstimmt, und schließen sich bei ATR-basierten Take-Profit- oder Stop-Loss-Niveaus.

## Details

- **Einstiegskriterien**: Preis über/unter der schnellen EMA, VWAP und Trendfilter.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: ATR-Take-Profit oder Stop-Loss.
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaFastPeriod` = 9
  - `EmaSlowPeriod` = 21
  - `AtrPeriod` = 14
  - `TakeProfitAtrMultiplier` = 0.7
  - `StopLossAtrMultiplier` = 1.4
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: EMA, VWAP, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (1m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
