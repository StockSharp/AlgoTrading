# CoeffofLine True-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie portiert den MQL5-Experten `Exp_CoeffofLine_true.mq5` auf das StockSharp-Framework. Sie verfolgt die **Steigung der linearen Regression** von Medianpreisen und reagiert auf Nulldurchgänge.

Eine Long-Position wird eröffnet, wenn die Steigung nach einem negativen Wert positiv wird. Eine Short-Position wird eröffnet, wenn die Steigung nach einem positiven Wert negativ wird. Bestehende Positionen werden bei entgegengesetzten Signalen geschlossen. Es werden nur abgeschlossene Kerzen verarbeitet.

## Parameter

- **Candle Type** – Zeitrahmen für die Kerzenserie.
- **Slope Period** – Länge der linearen Regression zur Berechnung der Steigung.
- **Signal Bar** – Historischer Bar-Index für die Signalauswertung.
- **Buy Open / Sell Open** – Berechtigungen zum Öffnen von Long- oder Short-Positionen.
- **Buy Close / Sell Close** – Berechtigungen zum Schließen von Long- oder Short-Positionen.

Die Strategie abonniert Kerzen, bindet den Indikator über die High-Level-API und arbeitet ohne manuelle Abfragen von Indikatorwerten.
