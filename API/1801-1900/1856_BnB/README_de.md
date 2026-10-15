# BnB Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist ein Port des MetaTrader 5 Expert Advisors "Exp_BnB". Sie verwendet den benutzerdefinierten BnB (Bulls and Bears)-Indikator, der bullischen und bärischen Druck innerhalb jeder Kerze misst und diese mit einem exponentiellen gleitenden Durchschnitt glättet.

## Funktionsweise

1. Für jede abgeschlossene Kerze berechnet die Strategie die Bulls- und Bears-Werte.
2. Beide Reihen werden mit EMA geglättet.
3. Wenn die Bulls-Linie die Bears-Linie nach oben kreuzt:
   - Jede Short-Position wird geschlossen.
   - Eine Long-Position wird eröffnet.
4. Wenn die Bears-Linie die Bulls-Linie nach oben kreuzt:
   - Jede Long-Position wird geschlossen.
   - Eine Short-Position wird eröffnet.
5. Stop-Loss- und Take-Profit-Niveaus werden in absoluten Preispunkten verwaltet.

## Parameter

- `Candle Type` – Zeitrahmen der für Berechnungen verwendeten Kerzen.
- `EMA Length` – Glättungsperiode für Bulls und Bears.
- `Stop Loss` – Abstand zum Schutz-Stop in Preispunkten.
- `Take Profit` – Abstand zum Gewinnziel in Preispunkten.
- `Allow Long Entry` – Long-Positionseröffnung aktivieren.
- `Allow Short Entry` – Short-Positionseröffnung aktivieren.
- `Allow Long Exit` – Long-Positionsschließung aktivieren.
- `Allow Short Exit` – Short-Positionsschließung aktivieren.

## Hinweise

Der ursprüngliche Indikator unterstützt mehrere Glättungsmethoden. In diesem Port wird der universelle Filter mit einem Standard-EMA approximiert.
