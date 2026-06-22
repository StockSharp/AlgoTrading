# CandleStop Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie implementiert Trailing-Stop-Management basierend auf dem CandleStop-Ansatz. Sie analysiert abgeschlossene Kerzen und bewegt den Stop-Level nur in Richtung des Trades. Der Algorithmus basiert auf Donchian-Kanälen mit separaten Rückblickperioden für Long- und Short-Positionen, was ihn für die Anbindung an manuelle Trades oder andere Einstiegsstrategien geeignet macht.

## Parameter
- **Up Trail Periods** – Anzahl der Kerzen zur Berechnung des höchsten Hochs für das Trailing von Short-Positionen.
- **Down Trail Periods** – Anzahl der Kerzen zur Berechnung des niedrigsten Tiefs für das Trailing von Long-Positionen.
- **Candle Type** – Zeitrahmen der für die Analyse verwendeten Kerzen.

## Strategielogik
1. Auf eine bestehende Position warten. Die Strategie eröffnet keine eigenen Trades.
2. Für Long-Positionen:
   - Das niedrigste Tief über *Down Trail Periods* berechnen.
   - Stop auf diesen Wert verschieben, wenn er höher als der vorherige Stop ist.
   - Wenn der Preis den Stop berührt oder darunter fällt, Position mit einer Marktorder schließen.
3. Für Short-Positionen:
   - Das höchste Hoch über *Up Trail Periods* berechnen.
   - Stop auf diesen Wert verschieben, wenn er niedriger als der vorherige Stop ist.
   - Wenn der Preis den Stop berührt oder darüber steigt, Position mit einer Marktorder zurückkaufen.

## Verwendungshinweise
- Konzipiert für die Verwendung mit der StockSharp High-Level-API und Kerzen-Abonnements.
- Geeignet zum Schutz von manuell oder durch andere Strategien eröffneten Positionen.
- Die Chartausgabe umfasst Kerzen, Kanallinien und ausgeführte Trades zur Visualisierung.
