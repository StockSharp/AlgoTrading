# Fractal WPR Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Williams %R Oszillator, um Handelssignale auf Basis von Kreuzungen der Überkauft- und Überverkauft-Niveaus zu generieren. Sie wurde aus einem MQL5 Expert Advisor adaptiert und demonstriert ein einfaches Momentum-Umkehr-System.

## Funktionsweise

1. Ein Williams %R Indikator mit konfigurierbarer Periode wird auf dem ausgewählten Zeitrahmen berechnet.
2. Zwei horizontale Niveaus definieren die Extremzonen:
   - `HighLevel` markiert den Überkauft-Bereich (Standard −30).
   - `LowLevel` markiert den Überverkauft-Bereich (Standard −70).
3. Wenn `Trend` auf `Direct` gesetzt ist:
   - Ein Abwärtskreuzen von `LowLevel` öffnet eine Long-Position und schließt alle Short-Positionen.
   - Ein Aufwärtskreuzen von `HighLevel` öffnet eine Short-Position und schließt alle Long-Positionen.
4. Wenn `Trend` auf `Against` gesetzt ist, werden die Reaktionen auf Kreuzungen umgekehrt.
5. Optionale Parameter ermöglichen das separate Aktivieren oder Deaktivieren des Öffnens und Schließens von Long- oder Short-Positionen.
6. Stop‑Loss- und Take‑Profit-Abstände in Ticks werden über die High-Level-Schutz-API angewendet.

Nur abgeschlossene Kerzen werden verarbeitet, um auf Intrabar-Rauschen nicht zu reagieren.

## Parameter

- `WprPeriod` – Berechnungsperiode von Williams %R.
- `HighLevel` – Schwellenwert für die Überkauft-Zone.
- `LowLevel` – Schwellenwert für die Überverkauft-Zone.
- `Trend` – Handelsmodus (`Direct` oder `Against`).
- `BuyPositionOpen` – Öffnen von Long-Positionen erlauben.
- `SellPositionOpen` – Öffnen von Short-Positionen erlauben.
- `BuyPositionClose` – Schließen von Long-Positionen erlauben.
- `SellPositionClose` – Schließen von Short-Positionen erlauben.
- `StopLossTicks` – Stop‑Loss-Abstand in Ticks.
- `TakeProfitTicks` – Take‑Profit-Abstand in Ticks.
- `CandleType` – Kerzen-Zeitrahmen für die Analyse.

## Indikatoren

- Williams %R
