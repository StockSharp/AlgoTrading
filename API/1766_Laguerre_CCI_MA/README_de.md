# Strategie Laguerre CCI MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den Laguerre-Filter, den Commodity Channel Index (CCI) und einen exponentiellen gleitenden Durchschnitt kombiniert.

## Übersicht
- Der Laguerre-Filter hebt überkaufte und überverkaufte Extremwerte auf einer 0-1-Skala hervor.
- Der CCI bestätigt den Momentum in derselben Richtung.
- Die EMA-Steigung filtert Trades entsprechend dem vorherrschenden Trend.

## Einstiegsregeln
- **Long**, wenn der Laguerre-Wert 0 ist, die EMA steigt und der CCI unter dem negativen `CciLevel`-Schwellenwert liegt.
- **Short**, wenn der Laguerre-Wert 1 ist, die EMA fällt und der CCI über dem positiven `CciLevel`-Schwellenwert liegt.

## Ausstiegsregeln
- Long-Positionen schließen, wenn Laguerre 0.9 überschreitet.
- Short-Positionen schließen, wenn Laguerre unter 0.1 fällt.

## Parameter
- `LagGamma` – Gamma-Wert für den Laguerre-Filter.
- `CciPeriod` – Periode für die CCI-Berechnung.
- `CciLevel` – absolutes CCI-Niveau für Einstiege.
- `MaPeriod` – Periode für den gleitenden Durchschnitt.
- `TakeProfit` – Take-Profit in absoluten Preiseinheiten (optional).
- `StopLoss` – Stop-Loss in absoluten Preiseinheiten (optional).
- `CandleType` – Kerzentyp, der für Indikatoren verwendet wird.

Die Strategie verarbeitet nur abgeschlossene Kerzen und verwendet die High-Level-API-Bindings von StockSharp für Indikatoren.
