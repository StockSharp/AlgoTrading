# Precision Trading: Golden Edge
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

EMA-Kreuzungen mit Bestätigung durch Hull-Richtung, RSI-Momentum und ATR/Hoch-Tief-Volatilität. Die ursprüngliche Beschreibung betrifft Gold-Scalping; das Beispiel verwendet das konfigurierte Instrument und belegt keine Ergebnisse für Gold.

## Signale und Ausführung

Nur abgeschlossene Kerzen, standardmäßig fünf Minuten. Alle sieben Indikatoren müssen gebildet sein. Die erste vollständig vorbereitete Kerze speichert vorherige EMA/HMA-Werte ohne Einstieg.

- Long: vorherige schnelle EMA ≤ vorherige langsame EMA, aktuelle schnelle EMA > aktuelle langsame EMA, aktuelle HMA > vorherige HMA und RSI > `RsiLongThreshold` (55).
- Short: vorherige schnelle EMA ≥ vorherige langsame EMA, aktuelle schnelle EMA < aktuelle langsame EMA, aktuelle HMA < vorherige HMA und RSI < `RsiShortThreshold` (45).
- Beide Richtungen verlangen positive close und ATR, `100 * ATR / close >= MinAtrPercent` (0.05) und `Highest(high) - Lowest(low) >= ATR * RangeAtrMultiplier` (3). Das Extremfenster enthält die aktuelle Kerze.

Ein bestätigtes Signal eröffnet `Volume` aus einer Nullposition oder sendet `Volume + abs(Position)` zur vollständigen Umkehr einer Gegenposition. Gleichgerichtete Signale halten die Position. Ausstehende Orders verhindern doppelte Einreichungen. Vorherige EMA/HMA-Werte werden bei jeder vorbereiteten Kerze aktualisiert; abgelehnte Kreuzungen werden nicht für später vorgemerkt. Es gibt keine sechsstündige Wartezeit und keine zusätzlichen Stop-Loss-/Take-Profit-Regeln. Eine bestätigte Gegenkreuzung kehrt die Position um.

## Indikatoren und Standardwerte

| Parameter | Standard | Bedeutung |
|---|---:|---|
| `FastLength` / `SlowLength` | 14 / 40 | Native Schlusskurs-EMA; schnell < langsam |
| `HmaLength` | 55 | `WMA(floor(sqrt(L)), 2 * WMA(floor(L/2), close) - WMA(L, close))`; L ≥ 2 |
| `RsiLength` | 14 | Nativer RSI mit Wilder-geglätteten Kursgewinnen/-verlusten |
| `RsiLongThreshold` / `RsiShortThreshold` | 55 / 45 | Strikte Schwellen; `0 <= short < long <= 100` |
| `AtrLength` | 14 | Nativer Wilder-ATR einschließlich Lücken zum vorherigen Schlusskurs |
| `RangeLength` | 20 | Höchstes high und niedrigstes low einschließlich aktueller Kerze |
| `MinAtrPercent` | 0.05 | Inklusive ATR-Untergrenze in Prozent des Schlusskurses; nicht negativ |
| `RangeAtrMultiplier` | 3 | Inklusive Untergrenze der Hoch-Tief-Spanne in ATR-Einheiten; positiv |
| `CandleType` | 5 Minuten | Abgeschlossene Kerzen für sämtliche Indikatoren |

WMA-Gewichte steigen von 1 für den ältesten Wert bis zur Fensterlänge für den neuesten. EMA beginnt mit dem nativen arithmetischen Mittel und verwendet danach `2 / (length + 1)`. ATR beginnt mit dem Mittel eines wachsenden Fensters und nutzt anschließend Wilder-Glättung. Perioden sind positiv; Hull benötigt auch die abschließende WMA mit ganzzahliger Wurzellänge.

C#- und Python-Tests berechnen EMA, HMA, RSI, ATR und gleitende Extrema unabhängig anhand des vorhandenen BTC/TON-Archivs. Sie prüfen jede Order, vollständige Umkehrungen, abgelehnte Bestätigungen und Signale mit weniger als sechs Stunden Abstand. Das prüft Verhalten, keine Rentabilität oder Eignung für den realen Handel.
