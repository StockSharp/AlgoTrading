# ColorMETRO XRSX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Implementierung, inspiriert vom originalen MQL5 Expert Advisor "Exp_ColorMETRO_XRSX". Sie verwendet zwei geglättete gleitende Durchschnitte, um Trendwechsel zu erkennen. Eine Long-Position wird eröffnet, wenn der schnelle Durchschnitt den langsamen von unten kreuzt, während eine Short-Position eröffnet wird, wenn der schnelle Durchschnitt den langsamen von oben kreuzt.

## Parameter

- **Fast Period** – Periode des schnellen gleitenden Durchschnitts.
- **Slow Period** – Periode des langsamen gleitenden Durchschnitts.
- **Candle Type** – Zeitrahmen der für Berechnungen verwendeten Kerzen.

## Funktionsweise

1. Die Strategie abonniert die ausgewählte Kerzenserie.
2. Zwei `Sma`-Indikatoren mit unterschiedlichen Perioden werden auf den Schlusskurs berechnet.
3. Wenn der schnelle SMA den langsamen SMA von unten kreuzt, wird eine offene Short-Position geschlossen und eine Long-Position eröffnet.
4. Wenn der schnelle SMA den langsamen SMA von oben kreuzt, wird eine offene Long-Position geschlossen und eine Short-Position eröffnet.
5. Die vorherigen Werte der Durchschnitte werden gespeichert, um Kreuzungen nur einmal zu erkennen.

## Hinweise

- Die Strategie verwendet die High-Level-API mit `Bind` für die Indikatorverarbeitung.
- `StartProtection` ist aktiviert, um Schutzmechanismen zu verwalten.
- Diese Implementierung ist eine vereinfachte Übersetzung und verwendet nicht den originalen benutzerdefinierten Indikator.
