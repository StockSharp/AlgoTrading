# Rohöl-Prognostiziert-Aktien-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie nutzt die Beziehung zwischen Rohöl und Aktienrenditen. Wenn die Rendite von Rohöl im vergangenen Monat positiv ist, investiert die Strategie in einen Aktien-ETF. Andernfalls rotiert das Kapital in einen Cash- oder Anleihen-ETF und bleibt aus Aktien heraus, wenn Öl schwach ist.

Der Algorithmus überwacht Tageskerzen und prüft das Signal am ersten Handelstag jedes Monats. Orders werden zu Marktpreisen eingereicht und respektieren eine Mindesthandelsgröße, um kleine Ausführungen zu vermeiden.

## Details

- **Universum**: Ein Aktien-ETF, ein Rohölinstrument und ein Cash- oder Anleihen-ETF.
- **Signal**: Long im Aktien-ETF, wenn die `Lookback`-Periodenrendite von Rohöl größer als null ist; andernfalls Cash-ETF halten.
- **Rebalancing**: Monatlich, zu Monatsbeginn.
- **Positionierung**: Long Aktien oder Cash, niemals beides.
- **Parameter**:
  - `Equity` – Ziel-Aktien-ETF.
  - `Oil` – Rohölinstrument für das Signal.
  - `CashEtf` – Defensivanlage bei negativer Ölrendite.
  - `Lookback` – Anzahl der Kerzen zur Berechnung der Ölrendite.
  - `CandleType` – Kerzen-Zeitrahmen (Standard: 1 Tag).
- **Hinweis**: Das Beispiel konzentriert sich auf die Struktur und lässt Transaktionskosten und Slippage außer Acht.
