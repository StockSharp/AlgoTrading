# Fibo Gleitender Durchschnitt Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie konvertiert den MetaTrader Expert Advisor **EA_Fibo_Avg_001a** in das StockSharp-Framework.
Sie verwendet zwei geglättete gleitende Durchschnitte. Die Länge des langsamen Durchschnitts ist die Summe des Basiszeitraums und eines Fibonacci-basierten Versatzes.
Eine Long-Position wird eröffnet, wenn der schnelle Durchschnitt den langsamen nach oben kreuzt, während eine Short-Position bei der entgegengesetzten Kreuzung eröffnet wird.
Positionen werden mit Stop-Loss, Take-Profit und einem Trailing Stop verwaltet. Optionales Money Management kann das Ordervolumen aus der Portfoliogröße berechnen.

## Parameter
- `CandleType` – Kerzen-Datentyp.
- `FiboNumPeriod` – zusätzliche Länge, die zum langsamen gleitenden Durchschnitt addiert wird.
- `MaPeriod` – Basiszeitraum der gleitenden Durchschnitte.
- `TrailingStop` – Trailing-Abstand in Preisschritten.
- `TakeProfit` – Take-Profit-Abstand in Preisschritten.
- `StopLoss` – Stop-Loss-Abstand in Preisschritten.
- `UseMoneyManagement` – einfaches Money Management aktivieren.
- `PercentMm` – Portfolioprozentsatz bei aktiviertem Money Management.
- `LotSize` – Standard-Ordervolumen bei deaktiviertem Money Management.

## Logik
1. Kerzen abonnieren und zwei geglättete gleitende Durchschnitte berechnen.
2. Wenn der schnelle Durchschnitt den langsamen nach oben kreuzt, kaufen. Wenn er nach unten kreuzt, verkaufen.
3. Nach dem Einstieg in eine Position Stop-Loss-, Take-Profit- und Trailing-Levels setzen.
4. Trailing Stop aktualisieren, wenn sich der Preis zugunsten der Position bewegt, und Positionen schließen, wenn Schutzlevels erreicht werden oder die entgegengesetzte Kreuzung auftritt.
