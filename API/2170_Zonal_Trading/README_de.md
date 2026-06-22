# Zonal-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie verwendet den Awesome Oscillator (AO) und den Accelerator Oscillator (AC), um Veränderungen im Marktimpuls zu erfassen.

## Logik
- Kaufen, wenn sowohl AO als auch AC über ihre vorherigen Werte steigen und mindestens einer von ihnen vom vorherigen Bar aufwärts gedreht hat, während beide Oszillatoren positiv sind.
- Verkaufen, wenn sowohl AO als auch AC unter ihre vorherigen Werte fallen und mindestens einer von ihnen vom vorherigen Bar abwärts gedreht hat, während beide Oszillatoren negativ sind.
- Long-Position schließen, wenn AO und AC abwärts drehen.
- Short-Position schließen, wenn AO und AC aufwärts drehen.

## Parameter
- **Candle Type** – Quellenkerzen-Serie für Berechnungen.
- **Take Profit** – fester Take-Profit-Wert in Preiseinheiten.

Die Strategie handelt immer nur eine Position gleichzeitig und verwendet Marktaufträge.
