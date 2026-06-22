# Lossless MA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Kreuzungen zwischen einem schnellen und einem langsamen Simple Moving Average (SMA).
Optional vermeidet sie die Realisierung von Verlusten, indem sie Verlustpositionen auf Break-even verschiebt, wenn das entgegengesetzte Signal erscheint.

## Funktionsweise

1. **Indikatoren**
   - Schneller SMA
   - Langsamer SMA
2. **Einstiege**
   - **Long** wenn `Schneller SMA > Langsamer SMA` und die aktuelle Richtung nicht Long ist.
   - **Short** wenn `Schneller SMA < Langsamer SMA` und die aktuelle Richtung nicht Short ist.
   - Zusätzliche Einstiege sind erlaubt, wenn `Close Losses` deaktiviert ist und die Anzahl der offenen Positionen unter `Max Deals` liegt.
3. **Ausstiege**
   - Bei einem entgegengesetzten Kreuzungssignal.
   - Wenn `Close Losses` aktiviert ist, wird die Position sofort geschlossen.
   - Wenn `Close Losses` deaktiviert ist und der Trade im Verlust liegt, wird eine Limit-Order zum Einstiegspreis gesetzt, um bei Break-even auszusteigen.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `FastLength` | Periode des schnellen SMA. | `10` |
| `SlowLength` | Periode des langsamen SMA. | `30` |
| `MaxDeals` | Maximale Anzahl gleichzeitiger Positionen. | `5` |
| `CloseLosses` | Verlustpositionen sofort schließen. | `true` |
| `Volume` | Ordervolumen. | `1` |
| `CandleType` | Kerzen für Berechnungen. | `1-minute` |

## Hinweise

Die Strategie verwendet Marktorders für Ein- und Ausstiege. Wenn `CloseLosses` deaktiviert ist, versucht sie Positionen zu schützen, indem sie eine Limit-Order zum Einstiegspreis platziert, anstatt mit Verlust zu schließen.
