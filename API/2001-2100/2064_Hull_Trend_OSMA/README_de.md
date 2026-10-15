# Hull Trend OSMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine Konvertierung des MetaTrader-Expertenberaters "Exp_HullTrendOSMA".

## Überblick

Die Strategie verwendet den Hull Trend OSMA-Indikator, der einen Hull Moving Average und eine geglättete Version davon berechnet. Der Oszillatorwert ist die Differenz zwischen diesen beiden Reihen. Wenn der Oszillator bei zwei aufeinanderfolgenden abgeschlossenen Kerzen steigt, öffnet die Strategie eine Long-Position. Wenn der Oszillator bei zwei aufeinanderfolgenden abgeschlossenen Kerzen fällt, öffnet die Strategie eine Short-Position. Entgegengesetzte Positionen werden bei jedem Signal geschlossen.

## Parameter

- **Hull Period** – Periode für den Hull Moving Average.
- **Signal Period** – Periode des auf den Oszillator angewendeten Glättungs-Moving-Average.
- **Take Profit** – Abstand für Take-Profit-Schutzaufträge in Preiseinheiten.
- **Stop Loss** – Abstand für Stop-Loss-Schutzaufträge in Preiseinheiten.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen (Standard 8 Stunden).

## Hinweise

- Verwendet die High-Level-StockSharp-API mit automatischer Kerzenabonnierung.
- Ein- und Ausstiege werden mit Marktaufträgen ausgeführt.
- Stop-Loss- und Take-Profit-Schutz wird einmalig beim Start der Strategie initialisiert.
