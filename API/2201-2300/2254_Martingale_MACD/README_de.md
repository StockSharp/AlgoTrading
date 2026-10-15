# Martingale MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den originalen MQL-Expertenberater "MartGreg_1" im StockSharp-Framework. Sie verwendet zwei Moving Average Convergence Divergence (MACD)-Indikatoren zur Suche nach Umkehrungen und wendet eine Martingale-Regel für die Positionsgrößenbestimmung an.

## Funktionsweise

- Der erste MACD sucht auf den letzten drei abgeschlossenen Kerzen nach lokalen Extremwerten.
- Der zweite MACD vergleicht die letzten zwei Werte, um die Momentumrichtung zu bestimmen.
- Eine Long-Position wird eröffnet, wenn der erste MACD ein Tal bildet und der zweite MACD abnimmt.
- Eine Short-Position wird eröffnet, wenn der erste MACD eine Spitze bildet und der zweite MACD zunimmt.
- Nach jedem verlustbringenden Trade wird die nächste Ordergröße bis zum konfigurierten Limit verdoppelt.
- Stop-Loss und Take-Profit werden in absoluten Preispunkten festgelegt.

## Parameter

- `Shape` – Divisor zur Berechnung des Anfangsvolumens aus dem Kontostand.
- `Doubling Count` – maximale Anzahl aufeinanderfolgender Verdopplungen nach Verlusten.
- `Stop Loss` – Schutz-Stop in Punkten.
- `Take Profit` – Gewinnziel in Punkten.
- `MACD1 Fast/Slow` – Perioden für den ersten MACD.
- `MACD2 Fast/Slow` – Perioden für den zweiten MACD.
- `Candle Type` – Zeitrahmen für die Analyse.

