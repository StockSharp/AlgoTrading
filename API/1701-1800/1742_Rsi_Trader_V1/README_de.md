# RSI Trader V1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Relative Strength Index (RSI), um Umkehrungen nach kurzfristigen Extremen zu identifizieren. Ein Kaufsignal tritt auf, wenn der RSI nach zwei aufeinanderfolgenden Kerzen unterhalb der Überverkauft-Schwelle diese nach oben kreuzt. Ein Verkaufssignal tritt auf, wenn der RSI nach zwei Kerzen oberhalb der Überkauft-Schwelle diese nach unten kreuzt. Die Strategie schließt optional eine bestehende entgegengesetzte Position und handelt nur innerhalb eines konfigurierbaren Zeitfensters.

## Details

- **Einstiegskriterien**:
  - **Long**: `RSI > BuyPoint` und RSI der vorherigen zwei Kerzen `< BuyPoint`.
  - **Short**: `RSI < SellPoint` und RSI der vorherigen zwei Kerzen `> SellPoint`.
- **Ausstiegskriterien**: Entgegengesetztes Signal oder Schutz-Stop/Take-Profit.
- **Zeitfilter**: Handelt nur, wenn die Eröffnungsstunde der Kerze zwischen `StartHour` und `EndHour` liegt.
- **Stops**: Feste Take Profit- und Stop Loss-Niveaus in Preiseinheiten.
- **Parameter**:
  - `RsiPeriod` – RSI-Berechnungsperiode.
  - `BuyPoint` – Überverkauft-Niveau für Long-Einstiege.
  - `SellPoint` – Überkauft-Niveau für Short-Einstiege.
  - `CloseOnOpposite` – aktuelle Position schließen, wenn entgegengesetztes Signal erscheint.
  - `StartHour` / `EndHour` – Handelszeiten.
  - `TakeProfit` / `StopLoss` – Schutzniveaus im Preis.

Dieses Beispiel demonstriert ein minimalistisches RSI-Crossover-System, das mit der High-Level-StockSharp-API erstellt wurde. Es kann als Vorlage für weitere Experimente verwendet werden.
