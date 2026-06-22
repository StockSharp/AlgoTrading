# Limits RSI Momentum Bot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
Diese Strategie platziert Limit-Orders basierend auf dem Relative Strength Index (RSI) und Momentum-Indikatoren. Sie zielt darauf ab, zu Rabatten zu kaufen und zu Prämien zu verkaufen, indem ausstehende Orders anstelle von Marktausführungen verwendet werden.

## Handelsregeln
- Operiert nur während des angegebenen Zeitfensters.
- Bei jeder abgeschlossenen Kerze werden RSI- und Momentum-Werte berechnet.
- Eine **Kauf-Limit-Order** wird unterhalb der Kerzen-Eröffnung platziert, wenn RSI und Momentum beide unter ihren Kaufschwellen liegen.
- Eine **Verkauf-Limit-Order** wird oberhalb der Kerzen-Eröffnung platziert, wenn RSI und Momentum beide über ihren Verkaufsschwellen liegen.
- Wenn eine Position eröffnet wird, wird die entgegengesetzte ausstehende Order storniert.
- Stop-Loss und Take-Profit werden automatisch über `StartProtection` verwaltet.

## Parameter
- `Volume` – Ordervolumen.
- `LimitOrderDistance` – Abstand in Preisschritten von der Kerzen-Eröffnung zum Platzieren ausstehender Orders.
- `TakeProfit` – Gewinnziel in Preisschritten.
- `StopLoss` – Verlustlimit in Preisschritten.
- `RsiPeriod` – Periode für RSI-Berechnung.
- `RsiBuyRestrict` / `RsiSellRestrict` – RSI-Schwellen, die Long- oder Short-Einstiege erlauben.
- `MomentumPeriod` – Periode für Momentum-Berechnung.
- `MomentumBuyRestrict` / `MomentumSellRestrict` – Momentum-Schwellen für Long- oder Short-Einstiege.
- `StartTime` / `EndTime` – Grenzen der Handelssitzung.
- `CandleType` – Kerzenintervall für Indikatorberechnungen.

## Hinweise
Die Strategie ist aus dem MQL4-Skript "The Limits Bot with RSI & Momentum" konvertiert und verwendet die High-Level-API von StockSharp.
