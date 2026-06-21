# Ergodic Ticks-Volumen-Indikator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wendet den True Strength Index (TSI) auf Kerzendaten an und vergleicht ihn mit einer exponentiellen gleitenden Durchschnittssignallinie. Eine Long-Position wird eröffnet, wenn der TSI die Signallinie von unten nach oben kreuzt, während eine Short-Position eröffnet wird, wenn er darunter kreuzt.

## Parameter

- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.
- **Short Length** – schnelle Glättungsperiode des TSI.
- **Long Length** – langsame Glättungsperiode des TSI.
- **Signal Length** – Periode des EMA, der als Signallinie verwendet wird.

## Logik

1. Kerzen des gewählten Zeitrahmens abonnieren.
2. TSI für jede abgeschlossene Kerze berechnen.
3. TSI durch einen EMA verarbeiten, um eine Signallinie zu erhalten.
4. Wenn der TSI die Signallinie von unten nach oben kreuzt, Long eingehen (dabei eventuell bestehende Short-Position schließen).
5. Wenn der TSI die Signallinie von oben nach unten kreuzt, Short eingehen (dabei eventuell bestehende Long-Position schließen).

Die Strategie ist eine Anpassung des MQL-Beispiels "exp_ergodic_ticks_volume_indicator.mq5" und verwendet ausschließlich integrierte StockSharp-Indikatoren.
