# Brake Exp-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt auf Basis des **BrakeExp**-Indikators. Der Indikator zeichnet einen adaptiven Unterstützungs- und Widerstandskanal, der aus einer Exponentialkurve aufgebaut ist. Ein Wechsel des Kanals von der unteren zur oberen Linie erzeugt ein Verkaufssignal, und ein Wechsel von der oberen zur unteren Linie erzeugt ein Kaufsignal.

## Funktionsweise

- Wenn der Indikator ein **Aufwärtssignal** meldet, schließt die Strategie Short-Positionen und eröffnet eine neue Long-Position.
- Wenn ein **Abwärtssignal** erscheint, werden bestehende Long-Positionen geschlossen und eine Short-Position eröffnet.
- Wenn nur ein **Aufwärtstrend** erkannt wird, schließt die Strategie Short-Positionen.
- Wenn nur ein **Abwärtstrend** erkannt wird, schließt die Strategie Long-Positionen.

Signale werden nur auf abgeschlossenen Kerzen verarbeitet.

## Parameter

- `A` – Kurvenbesch­leunigungsfaktor des BrakeExp-Indikators.
- `B` – Preisschritt für die Kanalbreite.
- `CandleType` – Kerzenserie für die Indikatorberechnung.
- `Volume` – Ordervolumen beim Markteintritt.

## Hinweise

Die Strategie nutzt die High-Level-API von StockSharp und kann in Designer, Shell oder jedem anderen StockSharp-Produkt ausgeführt werden.
