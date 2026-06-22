# ADX MA Crossover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie reproduziert den Expertenberater "ADX & MA", indem sie einen geglätteten gleitenden Durchschnitt mit einem Average Directional Index (ADX)-Trendfilter kombiniert. Die Logik analysiert die letzten zwei abgeschlossenen Kerzen auf dem gewählten Zeitrahmen und reagiert erst, wenn sowohl der gleitende Durchschnitt als auch der ADX bestätigte Werte geliefert haben. Sie ist für Hedging-Einstiege konzipiert, aber auf einem Netto-Positionsmodell implementiert, das die Position automatisch umkehrt, wenn entgegengesetzte Signale erscheinen.

Der gleitende Durchschnitt wird auf dem Median-Preis jeder Kerze berechnet, was der MetaTrader-Version entspricht, die eine SMMA auf `(High + Low) / 2` verwendete. Der ADX-Schwellenwert verhindert Trades, wenn die Trendstärke schwach ist, und reduziert Fehlsignale aus kurzlebigen Kreuzungen.

## Einstiegslogik
- Warten, bis sowohl der geglättete gleitende Durchschnitt als auch der ADX endgültige Werte geliefert haben.
- Den Schlusskurs der vorherigen Kerze (`n-1`) relativ zum geglätteten MA-Wert derselben Kerze auswerten.
- Long gehen, wenn:
  - Der Schlusskurs der Kerze `n-1` über dem MA-Wert von `n-1` liegt.
  - Der Schlusskurs der Kerze `n-2` unter diesem MA-Wert lag (bullische Kreuzung), und
  - Der ADX-Wert der Kerze `n-1` größer oder gleich `AdxThreshold` ist.
- Short gehen, wenn die umgekehrten Bedingungen eintreten (bärische Kreuzung mit ADX-Bestätigung).
- Die Positionsgröße verwendet das `Volume` der Strategie plus den absoluten Wert jedes entgegengesetzten Engagements, um eine Umkehrung bei entgegengesetzten Signalen zu garantieren.

## Ausstiegslogik
Long-Trades werden geschlossen, wenn eine der folgenden Bedingungen ausgelöst wird:
- Der letzte bestätigte Schlusskurs (`n-1`) fällt wieder unter den geglätteten MA (entgegengesetzte Kreuzung).
- Der Preis erreicht die konfigurierte Long-Take-Profit-Distanz in Pips.
- Der Preis fällt auf die konfigurierte Long-Stop-Loss-Distanz in Pips.
- Der Trailing-Stop für Long-Trades sichert Gewinne, sobald sich der Preis `TrailingStopBuy` Pips über den Einstiegspreis bewegt hat.

Short-Trades spiegeln dieselben Regeln mit ihren jeweiligen Parametern und Trailing-Logik. Jedes Mal, wenn ein entgegengesetztes Signal erscheint, sendet die Strategie eine Marktorder, die groß genug ist, um die aktuelle Position zu schließen und eine in der neuen Richtung zu öffnen.

## Risiko- und Handelsmanagement
- Abstände für Take-Profit, Stop-Loss und Trailing-Stop werden in **Pips** ausgedrückt. Die Strategie leitet die Pip-Größe aus `Security.PriceStep` ab; wenn das Symbol 3 oder 5 Dezimalstellen verwendet, wird der Pip als `PriceStep × 10` definiert, entsprechend der ursprünglichen MetaTrader-Anpassung.
- `InitializeLongTargets` und `InitializeShortTargets` berechnen unmittelbar nach dem Senden der Marktorder absolute Preisniveaus und speichern die Einstiegspreisnäherung basierend auf dem letzten bestätigten Schlusskurs.
- Wenn Trailing-Stops aktiviert sind und sich der Preis günstig über die konfigurierte Distanz hinaus bewegt, wird das Stop-Niveau verschoben, um unrealisierte Gewinne zu erhalten.
- Beide Zielsätze werden zurückgesetzt, wenn die Position geschlossen wird, sodass veraltete Niveaus nie wiederverwendet werden.

## Parameter
- `MaPeriod` – Länge des geglätteten gleitenden Durchschnitts (Standard 15).
- `AdxPeriod` – ADX-Glättungsperiode (Standard 12).
- `AdxThreshold` – minimaler ADX-Wert zur Trendbestätigung (Standard 16).
- `TakeProfitBuy` / `StopLossBuy` / `TrailingStopBuy` – Pip-Abstände für Long-Trades.
- `TakeProfitSell` / `StopLossSell` / `TrailingStopSell` – Pip-Abstände für Short-Trades.
- `CandleType` – Zeitrahmen für Eingabekerzen, Standard 1 Minute.

Setzen Sie das `Volume` der Strategie, um die Basis-Ordergröße zu steuern. Die Implementierung behält das ursprüngliche Verhalten bei, bei dem Short-Trades ihre eigenen Risikoeinstellungen erhalten, anstatt die Long-Parameter zu verwenden.
