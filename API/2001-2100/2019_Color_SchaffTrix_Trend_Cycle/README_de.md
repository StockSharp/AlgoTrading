# Color Schaff TRIX Trend-Zyklus-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert den **Schaff Trend Cycle**-Oszillator, der auf dem TRIX-basierten MACD berechnet wird. Der Oszillator identifiziert zyklische Trendwechsel und generiert Handelssignale, wenn der Zyklus vordefinierte Niveaus kreuzt.

## Funktionsweise

1. Zwei TRIX-Oszillatoren mit unterschiedlichen Längen werden berechnet, um eine MACD-Serie aufzubauen.
2. Die MACD-Werte werden durch eine doppelte stochastische Transformation verarbeitet, um den Schaff Trend Cycle (STC) zu erzeugen.
3. Eine Long-Position wird eröffnet, wenn der STC das hohe Niveau nach oben kreuzt, und eine Short-Position, wenn er das niedrige Niveau nach unten kreuzt.
4. Bestehende Positionen werden bei einem entgegengesetzten Kreuz geschlossen.

## Parameter

- **Fast TRIX** – Länge des schnellen TRIX-Oszillators.
- **Slow TRIX** – Länge des langsamen TRIX-Oszillators.
- **Cycle** – Periode für die stochastischen Berechnungen.
- **High Level / Low Level** – obere und untere Schwellenwerte für den STC.
- **Stop Loss % / Take Profit %** – Risikosteuerungsparameter in Prozent.
- **Buy/Sell Open/Close** – entsprechende Operationen aktivieren oder deaktivieren.

## Hinweise

Die Strategie verwendet Kerzendaten des ausgewählten Zeitrahmens (Standard 4 Stunden) und führt Marktaufträge aus. Der Schutz ist mit Stop-Loss- und Take-Profit-Werten aktiviert. Die gesamte Indikatorverarbeitung erfolgt über die High-Level-API mit automatischen Bindungen.

Verwenden Sie diese Strategie zu Bildungszwecken und führen Sie vor dem Live-Handel gründliche Backtests durch.
