# Color HMA Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf Steigungsänderungen des Hull Moving Average. Sie schließt Positionen gegen die neue Richtung und eröffnet Positionen im Trend, wenn der HMA umkehrt.

## Parameter
- `HmaPeriod` — Periode für den Hull Moving Average.
- `CandleType` — Art der zu verwendenden Kerzen.
- `BuyOpen`, `SellOpen` — Eröffnung von Long-/Short-Positionen erlauben.
- `BuyClose`, `SellClose` — Schließen von Long-/Short-Positionen erlauben.

## Signale
- **Aufwärtsumkehr**: Vorheriger HMA fiel und aktueller Wert steigt → Shorts schließen und Long eröffnen.
- **Abwärtsumkehr**: Vorheriger HMA stieg und aktueller Wert fällt → Longs schließen und Short eröffnen.

Die Strategie verwendet Market-Orders und handelt mit dem in `Strategy.Volume` angegebenen Volumen.
