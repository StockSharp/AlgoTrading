# Nevalyashka-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie implementiert ein einfaches abwechselndes Long/Short-System mit Martingale-Positionsgröße.

## Strategielogik

1. Beim Start wird eine Short-Position eröffnet.
2. Ein fester Take-Profit und Stop-Loss werden der Position angehängt.
3. Jedes Mal, wenn die Position geschlossen wird (durch Stop oder Ziel):
   - Der nächste Trade wird in die entgegengesetzte Richtung eröffnet.
   - Wenn der vorherige Trade mit einem Verlust endete, wird das Ordervolumen mit `LotMultiplier` multipliziert.
   - Wenn der vorherige Trade mit einem Gewinn endete, wird das Volumen auf das Basis-`Volume` zurückgesetzt.
4. Schritte 2‑3 wiederholen sich unbegrenzt.

## Parameter

- `Volume` – Basis-Ordervolumen für den ersten Trade und nach Gewinn-Trades.
- `LotMultiplier` – Multiplikator, der nach einem Verlust-Trade auf das Volumen angewendet wird.
- `TakeProfit` – Gewinnzielabstand in Preispunkten.
- `StopLoss` – Stop-Loss-Abstand in Preispunkten.

## Hinweise

- Schutzorders werden über `StartProtection` verwaltet.
- Die Strategie stützt sich nicht auf Marktdaten; sie reagiert nur auf Positionsänderungen.
