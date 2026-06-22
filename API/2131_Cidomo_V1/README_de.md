# Cidomo V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Tages-Ausbruchsstrategie, die Trades platziert, wenn der Preis die jüngste Range verlässt.

## Zusammenfassung

- **Typ**: Ausbruch
- **Einstieg**: Kauf, wenn der Preis über das höchste Hoch des Rückblickzeitraums ausbricht; Verkauf, wenn der Preis unter das niedrigste Tief fällt.
- **Ausstieg**: Stop-Loss, Take-Profit, optionaler Breakeven und Trailing Stop.
- **Indikatoren**: Highest, Lowest

## Parameter

| Name | Beschreibung |
|------|--------------|
| `Lookback` | Anzahl der Kerzen zur Berechnung der Range. |
| `Delta` | Preisversatz, der zu den Ausbruchsniveaus addiert wird. |
| `StopLoss` | Stop-Loss in Preispunkten. |
| `TakeProfit` | Take-Profit in Preispunkten. |
| `NoLoss` | Stop nach diesem Gewinn (Punkte) auf Einstiegspreis verschieben. |
| `Trailing` | Trailing-Abstand in Punkten. |
| `UseTimeFilter` | Wenn true, werden die Niveaus nach der angegebenen Zeit berechnet. |
| `TradeTime` | Tageszeit zur Berechnung der Ausbruchsniveaus. |
| `CandleType` | Kerzentyp für die Berechnungen. |

## Hinweise

Die Strategie überwacht nur abgeschlossene Kerzen. Niveaus werden einmal täglich nach `TradeTime` neu berechnet.
