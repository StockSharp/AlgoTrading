# Dispersion-Trading-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Dispersion-Trading-Strategie nutzt Phasen aus, in denen ein Aktienindex und seine Bestandteile auseinanderdriften. Wenn die durchschnittliche paarweise Korrelation zwischen Indexmitgliedern unter einen Schwellenwert fällt, kauft die Strategie die Einzelaktien und verkauft den Index leer, in der Erwartung, dass die Korrelationen zur Mean Reversion neigen.

Tageskerzen speisen ein rollendes Korrelationsfenster. Erholen sich die Korrelationen über den Schwellenwert, werden alle Positionen geschlossen. Ein Mindesthandelswert wird durchgesetzt, um kleine Orders zu vermeiden.

## Details

- **Universum**: Ein Indexwertpapier plus seine Bestandteile.
- **Signal**: Dispersion-Trade eröffnen, wenn die durchschnittliche Korrelation der Bestandteile unter `CorrThreshold` liegt.
- **Rebalancing**: Korrelation täglich geprüft.
- **Positionierung**: Long Bestandteile und Short der Index, solange das Signal aktiv ist.
- **Parameter**:
  - `Constituents` – Liste der Komponenten-Wertpapiere.
  - `LookbackDays` – Fenstergröße für die Korrelationsberechnung.
  - `CorrThreshold` – Korrelationsniveau, das Trades auslöst.
  - `MinTradeUsd` – Mindestorderwert in USD.
  - `CandleType` – Zeitrahmen für Kerzen (Standard: 1 Tag).
- **Hinweis**: Das Beispiel lässt Transaktionskosten außer Acht und nimmt Gleichgewichtung an.
