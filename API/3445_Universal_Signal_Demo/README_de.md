# Universalsignal-Demo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert den MetaTrader 5 „Universal Signal“-Experten unter Verwendung von StockSharp High-Level-APIs. Es wertet acht gewichtete Marktmuster aus und fasst sie zu einem einzigen zusammengesetzten Score zusammen. Wenn der Score konfigurierbare Schwellenwerte überschreitet, öffnet oder schließt die Strategie Long- und Short-Positionen, optional unter Verwendung ausstehender Limit-Orders, die nach einer festgelegten Anzahl von Balken ablaufen.

## Strategieparameter
- `CandleType` – Kerzendaten, die für die Analyse verwendet werden.
- `SignalThresholdOpen` – minimale Gesamtpunktzahl, die zum Öffnen einer Position erforderlich ist.
- `SignalThresholdClose` – Gegenpunktzahl erforderlich, um eine bestehende Position zu verlassen.
- `PriceLevel` – Preisversatz für die Platzierung ausstehender Limiteinträge (0 bedeutet Marktausführung).
- `StopLevel` / `TakeLevel` – absolute Stop-Loss- und Take-Profit-Distanzen, die vom eingebauten Schutzmodul verwendet werden.
- `SignalExpiration` – Anzahl der Balken, nach denen noch aktive ausstehende Einträge storniert werden.
- `Pattern0Weight` … `Pattern7Weight` – Gewichtung, die vor der Aggregation auf jedes Muster angewendet wird.
- `UniversalWeight` – endgültiger Multiplikator, der auf die Summe aller Musterbeiträge angewendet wird.
- `ShortMaPeriod`, `LongMaPeriod`, `RsiPeriod`, `BollingerPeriod`, `BollingerWidth`, `TrendSmaPeriod`, `VolumeSmaPeriod` – Indikatoreinstellungen, die in den Musterprüfungen verwendet werden.

## Handelslogik
1. Abonnieren Sie den konfigurierten Kerzenstrom und binden Sie EMA, RSI, MACD Signal, Bollinger Bänder und unterstützende SMAs.
2. Berechnen Sie nach jeder fertigen Kerze acht boolesche Muster (Trendausrichtung, RSI-Momentum, MACD-Histogramm, Bollinger-Positionierung, Kerzenrichtung und Volumenexpansion).
3. Multiplizieren Sie jedes Muster mit seiner Gewichtung, summieren Sie die Beiträge und wenden Sie die globale Gewichtung an, um das Endergebnis zu erhalten.
4. Schließen Sie offene Positionen, wenn der Score die Schließschwelle in die entgegengesetzte Richtung überschreitet.
5. Eröffnen Sie neue Long- oder Short-Positionen, wenn der Score den Eröffnungsschwellenwert überschreitet. Wenn `PriceLevel` positiv ist, senden Sie eine um die konfigurierte Distanz versetzte Limit-Order und stornieren Sie diese automatisch nach `SignalExpiration` Balken.
6. `StartProtection` legt mithilfe der Risikomanagement-Helfer von StockSharp feste Stop-Loss- und Take-Profit-Levels für alle Positionen fest.

Bei der Konvertierung bleibt der flexible Gewichtungsworkflow des ursprünglichen MQL5-Experten erhalten, während die StockSharp-Codierungskonventionen und die indikatorbasierte Verarbeitung befolgt werden.
