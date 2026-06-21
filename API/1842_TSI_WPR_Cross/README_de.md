# TSI WPR-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des Crossovers des True Strength Index (TSI), der aus dem Williams %R-Oszillator berechnet wird.
Wenn der TSI über seine geglättete Signallinie kreuzt, eröffnet die Strategie eine Long-Position. Wenn der TSI unter die Signallinie kreuzt, eröffnet sie eine Short-Position.

## Parameter
- **Candle Type**: Zeitrahmen der für die Berechnung verwendeten Kerzen.
- **Williams %R Period**: Anzahl der Balken für den Williams %R-Indikator.
- **Short Length**: Kurze EMA-Länge, die in der TSI-Berechnung verwendet wird.
- **Long Length**: Lange EMA-Länge, die in der TSI-Berechnung verwendet wird.
- **Signal Length**: EMA-Länge, die auf den TSI angewendet wird, um die Signallinie zu bilden.

## Handelsregeln
1. Williams %R-Wert jeder abgeschlossenen Kerze berechnen.
2. Diesen Wert in den True Strength Index-Indikator einspeisen.
3. TSI mit einem EMA glätten, um die Signallinie zu erhalten.
4. **Kaufen**, wenn TSI über die Signallinie kreuzt.
5. **Verkaufen**, wenn TSI unter die Signallinie kreuzt.
6. Bestehende Positionen in entgegengesetzter Richtung werden bei einem neuen Signal geschlossen.

## Hinweise
- Die Strategie verwendet die High-Level-API mit automatischen Kerzenabonnements.
- StartProtection wird beim Start für das grundlegende Risikomanagement gestartet.
- Chartbereiche werden erstellt, um TSI, seine Signallinie und ausgeführte Trades zu visualisieren.
