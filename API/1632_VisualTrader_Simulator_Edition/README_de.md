# VisualTrader Simulator Edition
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine vereinfachte Portierung der VisualTrader-Skripte aus MetaTrader.

Sie eröffnet eine einzelne Marktposition in der gewählten Richtung und fügt schützende Stop-Loss- und Take-Profit-Orders hinzu. Die Parameter ermöglichen die Konfiguration von Richtung, Take-Profit und Stop-Loss in absoluten Preiswerten. Die Strategie demonstriert, wie manuelle Trade-Management-Skripte mit der High-Level-API von StockSharp nachgebaut werden können.

## Parameter

- **Trade Direction** – Buy oder Sell für die initiale Order wählen.
- **Take Profit** – optionaler Take-Profit-Wert in absolutem Preis. Auf 0 setzen zum Deaktivieren.
- **Stop Loss** – optionaler Stop-Loss-Wert in absolutem Preis. Auf 0 setzen zum Deaktivieren.
- **Volume** – Basis-Strategie-Volumen für die Marktorder.

## Handelslogik

Beim Start führt die Strategie folgendes aus:

1. Erstellt Schutzorders mit `StartProtection`.
2. Sendet eine Marktorder basierend auf der gewählten Handelsrichtung.

Das Beispiel ist nicht auf Indikatoren oder Marktdaten angewiesen und dient ausschließlich Demonstrationszwecken.
