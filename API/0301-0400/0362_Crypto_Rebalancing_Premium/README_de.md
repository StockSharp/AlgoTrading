# Krypto-Rebalancing-Prämien-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Krypto-Rebalancing-Prämien-Strategie hält ein gleichgewichtetes Portfolio aus Bitcoin und Ethereum. Durch wöchentliches Rebalancing versucht sie, die durch die Volatilität zwischen den beiden Assets generierte Prämie zu vereinnahmen.

Die Strategie überwacht Stundenkerzen und führt ein Rebalancing in der ersten Stunde jedes Montags durch. Trades werden übersprungen, wenn die erforderliche Anpassung kleiner als ein benutzerdefinierter USD-Schwellenwert ist.

## Details

- **Universum**: Bitcoin- und Ethereum-Symbole.
- **Signal**: BTC und ETH bei 50/50-Gewichtung halten.
- **Rebalancing**: Wöchentlich, montags um 00:00 UTC.
- **Positionierung**: Nur Long, gleichgewichtet.
- **Parameter**:
  - `BTC` – Bitcoin-Wertpapier.
  - `ETH` – Ethereum-Wertpapier.
  - `MinTradeUsd` – Mindesthandelswert in USD.
  - `CandleType` – Kerzen-Zeitrahmen (Standard: 1 Stunde).
- **Hinweis**: Die Implementierung ist vereinfacht und berücksichtigt keine Gebühren oder Finanzierungskosten.
