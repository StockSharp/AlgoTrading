# Color Momentum AMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie konvertiert den MetaTrader Expert Advisor *Exp_ColorMomentum_AMA* nach StockSharp.
Sie berechnet das Preismomentum über einen konfigurierbaren Zeitraum und glättet es mit dem Kaufman Adaptive Moving Average (AMA).
Handelssignale werden generiert, wenn das geglättete Momentum zwei aufeinanderfolgende Anstiege oder Rückgänge zeigt.

## Logik
- **Long-Einstieg**: Momentum AMA steigt zwei Kerzen hintereinander. Jede bestehende Short-Position wird vor dem Eröffnen einer neuen Long-Position geschlossen.
- **Short-Einstieg**: Momentum AMA fällt zwei Kerzen hintereinander. Jede bestehende Long-Position wird vor dem Eröffnen einer neuen Short-Position geschlossen.
- Entgegengesetzte Signale schließen aktuelle Positionen.

## Parameter
- Kerzentyp
- Momentum-Periode
- AMA-Periode
- Schnelle Periode
- Langsame Periode
- Signalkerze
