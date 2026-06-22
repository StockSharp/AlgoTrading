# EMA-Kreuzungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt die Kreuzung zweier exponentieller gleitender Durchschnitte (EMA).
Eine Long-Position wird eröffnet, wenn die schnelle EMA die langsame EMA von unten kreuzt, während eine Short-Position eröffnet wird, wenn die schnelle EMA die langsame EMA von oben kreuzt.
Der Parameter **Reverse** tauscht die EMA-Rollen, was die Einstiegssignale effektiv umkehrt.

Jede Position ist durch feste **Take Profit**- und **Stop Loss**-Niveaus geschützt.
Ein optionaler **Trailing Stop** folgt dem Preis, sobald er sich in die günstige Richtung bewegt, und sichert Gewinne.

Die Strategie verarbeitet nur abgeschlossene Kerzen und verwendet High-Level-API-Binding für Indikatoren und Kerzen-Subscriptions.

## Parameter
- Kerzentyp
- Länge der schnellen EMA
- Länge der langsamen EMA
- Take profit
- Stop loss
- Trailing stop
- Reverse
