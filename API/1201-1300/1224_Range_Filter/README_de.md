# Range-Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Range-Filter-Strategie mit realistischer Berechnung der Bandbreite und festen Risiko-/Renditeniveaus.

Es wird eine geglättete Bandbreite verwendet, um dynamische Bänder um den Preis zu erstellen. Trades werden eröffnet, wenn der Preis diese Bänder nach oben oder unten durchbricht. Das Risikomanagement verwendet feste Stop-Loss- und Take-Profit-Abstände.

## Details

- **Einstiegskriterien**: Preis bricht die Range-Filter-Bänder.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Stop Loss oder Take Profit.
- **Stops**: Ja.
- **Standardwerte**:
  - `SamplingPeriod` = 100
  - `RangeMultiplier` = 3
  - `RiskPoints` = 50
  - `RewardPoints` = 100
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: Range filter
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
