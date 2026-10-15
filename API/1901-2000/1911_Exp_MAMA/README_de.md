# Exp MAMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt mit dem MESA Adaptive Moving Average (MAMA)-Indikator.

Der Indikator erzeugt zwei Linien:

- **MAMA** – der adaptive gleitende Durchschnitt.
- **FAMA** – ein folgender Durchschnitt, der als Signallinie verwendet wird.

Handelslogik:

1. Wenn MAMA unter FAMA kreuzt, schließt die Strategie Short-Positionen und eröffnet eine neue Long-Position.
2. Wenn MAMA über FAMA kreuzt, schließt die Strategie Long-Positionen und eröffnet eine neue Short-Position.

## Parameter

- `FastLimit` – schnelles Alpha-Limit, das vom adaptiven Faktor verwendet wird.
- `SlowLimit` – langsames Alpha-Limit, das vom adaptiven Faktor verwendet wird.
- `CandleType` – Zeitrahmen für eingehende Kerzen.
- `BuyOpen` / `SellOpen` – erlauben das Öffnen von Long- oder Short-Positionen.
- `BuyClose` / `SellClose` – erlauben das Schließen von Long- oder Short-Positionen.

Die Strategie arbeitet mit abgeschlossenen Kerzen und verwendet Marktaufträge für Ein- und Ausstieg.
