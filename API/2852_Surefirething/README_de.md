# Surefirething-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Surefirething-Strategie recreiert den klassischen MetaTrader 5-Expertenberater, der symmetrische Buy- und Sell-Limit-Orders rund um den letzten Kerzenschlusskurs platziert. Das System baut das Grid nach jeder abgeschlossenen Kerze ständig neu auf, verwaltet Schutz-Stops in Pip-Einheiten und erzwingt zehn Minuten vor Mitternacht (Serverzeit) eine vollständig flache Position.

## Kerzenverarbeitung
- Funktioniert mit einem konfigurierbaren Kerzentyp (Standard: 1-Stunden-Zeitrahmen).
- Nach jeder abgeschlossenen Kerze berechnet die Strategie eine verstärkte Spanne: `range = (high - low) * 1.1`.
- Sie leitet zwei Ausbruchslevels aus dieser Spanne ab:
  - `L4 = close - range / 2` für die Buy-Limit-Order.
  - `H4 = close + range / 2` für die Sell-Limit-Order.
- Bestehende ausstehende Orders werden vor der Veröffentlichung des neuen Grids storniert, sodass nur eine Buy- und eine Sell-Limit-Order aktiv bleiben.

## Orderverwaltung
- Buy-Limit bei `L4` und Sell-Limit bei `H4` werden mit dem konfigurierten Ordervolumen registriert.
- Sobald sich eine Position öffnet, wird die entgegengesetzte ausstehende Order sofort storniert.
- Täglich um **23:50** (Serverzeit) führt die Strategie folgende Aktionen aus:
  - Storniert alle verbleibenden ausstehenden Orders.
  - Schließt die offene Position zum Marktpreis, falls vorhanden.
  - Setzt alle Stop/Take-Profit-Tracker zurück, um die nächste Session sauber zu beginnen.

## Risikomanagement
- Stop-Loss- und Take-Profit-Abstände werden in Pips definiert und mithilfe des Instrument-Preisschritts in Preise umgerechnet (5-stellige und 3-stellige Symbole werden automatisch auf klassische Pip-Einheiten angepasst).
- Ein Trailing-Stop (ebenfalls in Pips) kann aktiviert werden. Jedes Mal, wenn sich der Preis über `TrailingStopPips + TrailingStepPips` hinaus bewegt, wird der Stop auf `aktueller Preis - TrailingStopPips` für Longs oder `aktueller Preis + TrailingStopPips` für Shorts vorgeschoben.
- Beide Schutz-Levels werden bei jeder Kerze überwacht. Wenn die Kerze durch den Stop oder das Ziel handelt, verlässt die Strategie die Position mit Market-Orders.

## Parameter
- `OrderVolume` – Basisvolumen für beide Limit-Orders (Standard: `0.1`).
- `StopLossPips` – Stop-Loss-Abstand in Pips (Standard: `50`).
- `TakeProfitPips` – Take-Profit-Abstand in Pips (Standard: `50`).
- `TrailingStopPips` – Trailing-Stop-Abstand in Pips (Standard: `25`).
- `TrailingStepPips` – Zusätzliche Bewegung in Pips, die erforderlich ist, bevor sich der Trailing-Stop bewegt (Standard: `1`). Muss größer als null sein, wenn ein Trailing-Stop aktiviert ist.
- `CandleType` – Kerzendatentyp für Berechnungen (Standard: 1-Stunden-Zeitrahmen).

## Hinweise
- Die Implementierung entspricht der ursprünglichen MQL-Logik, indem sichergestellt wird, dass der Trailing-Schritt ungleich null ist, wenn Trailing aktiv ist.
- Für diese Strategie wird keine Python-Implementierung bereitgestellt.
