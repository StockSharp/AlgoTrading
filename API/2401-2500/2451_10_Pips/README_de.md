# 10-Pips-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Hedging-Strategie öffnet gleichzeitig Long- und Short-Positionen. Jede Position verwendet feste Take-Profit- und Stop-Loss-Niveaus gemessen in Preiseinheiten und kann durch einen Trailing Stop geschützt werden. Wenn eine Seite geschlossen wird, öffnet die Strategie sofort eine neue Position in dieselbe Richtung, um beide Seiten aktiv zu halten.

## Parameter
- `TakeProfitBuy` – Take-Profit-Abstand für Long-Positionen.
- `StopLossBuy` – Stop-Loss-Abstand für Long-Positionen.
- `TrailingStopBuy` – Trailing Stop-Abstand für Long-Positionen.
- `TakeProfitSell` – Take-Profit-Abstand für Short-Positionen.
- `StopLossSell` – Stop-Loss-Abstand für Short-Positionen.
- `TrailingStopSell` – Trailing Stop-Abstand für Short-Positionen.
- `Volume` – Ordergröße für alle Trades.

## Hinweise
- Positionen werden mit Marktorders eröffnet.
- Schutzorders werden für jede Seite separat registriert.
- Trailing Stops werden aktualisiert, wenn sich der Markt in einer günstigen Richtung bewegt.
