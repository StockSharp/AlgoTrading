# Länder-Wertfaktor-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Länder-Wertfaktor-Strategie ordnet Aktienmärkte nach dem Shiller-CAPE-Verhältnis. Länder mit dem niedrigsten CAPE gelten als günstig und werden gekauft, während teure Märkte gemieden werden. Der Ansatz nutzt die Tendenz unterbewerteter Märkte, langfristig besser abzuschneiden.

Jeden Monat verteilt die Strategie das Kapital gleichmäßig auf die günstigsten Länder aus einem benutzerdefinieren Universum. Positionen werden nach Portfoliowert bemessen und nur ausgeführt, wenn der Handel einen Mindest-USD-Betrag überschreitet.

## Details

- **Universum**: Kollektion von Länder-Aktien-ETFs.
- **Signal**: Kauf der Länder mit den niedrigsten CAPE-Verhältnissen.
- **Rebalancing**: Erster Handelstag jedes Monats.
- **Positionierung**: Nur Long.
- **Parameter**:
  - `Universe` – Wertpapiere, die jedes Land repräsentieren.
  - `MinTradeUsd` – Mindestdollarbetrag pro Order.
  - `CandleType` – Zeitrahmen der Kerzen (Standard: 1 Tag).
- **Hinweis**: Der Beispielcode enthält Platzhalterlogik und sollte mit echten Faktorberechnungen erweitert werden.
