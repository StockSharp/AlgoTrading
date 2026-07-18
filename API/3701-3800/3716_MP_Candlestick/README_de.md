# MP Candlestick-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MP Candlestick-Strategie** ist eine Umwandlung des MetaTrader 5 Expert Advisors `mp candlestick.mq5` in das StockSharp High-Level-Strategie-Framework. Das System bewertet die Richtung abgeschlossener Kerzen und eröffnet Geschäfte in die gleiche Richtung unter Anwendung eines strengen Risikomanagements. Es unterstützt sowohl feste Stop-Loss-Abstände, ausgedrückt in MetaTrader Pips, als auch adaptive Stop-Loss-Platzierungen, die aus der Average True Range (ATR) abgeleitet werden.

## Handelslogik
1. Die Strategie abonniert eine einzelne konfigurierbare Kerzenserie (Standard: 1-Stunden-Kerzen).
2. Auf jeder fertigen Kerze:
   - Bullische Kerze (Schlusskurs über Eröffnung) → Erwägen Sie eine Long-Position.
   - Bärische Kerze (Schlusskurs unter Eröffnung) → Erwägen Sie eine Short-Position.
   - Doji-Kerzen werden ignoriert.
3. Vor jedem Einstieg berechnet die Strategie einen Stop-Loss-Preis entweder aus ATR oder aus der festen Pip-Distanz. Der Take-Profit-Preis wird anhand des konfigurierten Risiko-Ertrags-Verhältnisses berechnet.
4. Wenn die Margin-Nutzung innerhalb des zulässigen Prozentsatzes bleibt und die berechnete Positionsgröße gültig ist, wird der Handel zum Marktwert eröffnet.
5. Während die Position aktiv ist, überwacht die Strategie jede neue Kerze auf Folgendes:
   - Stop-Loss- oder Take-Profit-Hits mithilfe von Candle-Extremen.
   - Trailing-Anpassung, die den Stop in Richtung Breakeven verschiebt, wenn ATR Stops aktiviert sind.
6. Sobald die Position flach ist, beginnt der Prozess mit der nächsten fertigen Kerze erneut.

## Risiko- und Geldmanagement
- **Risikoprozentsatz** definiert den Aktienanteil, der pro Trade riskiert wird. Die Positionsgröße ergibt sich aus dem Preisabstand zwischen Einstieg und Stop-Loss und dem Instrumentenpreis/Schrittwert.
- Das **Risiko-Ertrags-Verhältnis** bestimmt den Abstand zwischen Einstiegspreis und Take-Profit-Ziel im Verhältnis zum anfänglichen Risiko.
- **Max. Margin-Nutzung** schränkt ein, wie viel geschätzte Marge der neue Trade im Vergleich zum aktuellen Portfolio-Eigenkapital verbrauchen darf.
- **Trailing Stop** wird automatisch aktiviert, wenn ATR-basiertes Risikomanagement verwendet wird. Es verschiebt den Stop auf halbem Weg in Richtung des Gewinnziels, ohne den letzten Schlusskurs der Kerze zu überschreiten, und versucht so, Gewinne zu sichern und dabei die Wechselkursbeschränkungen zu respektieren.

## Parameter
| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `RiskPercent` | 1 | Prozentsatz des Portfolio-Eigenkapitals, der als maximaler Verlust für einen einzelnen Trade zugewiesen wird. |
| `RiskRewardRatio` | 1.5 | Multiplikator, der auf die anfängliche Risikodistanz angewendet wird, um das Take-Profit-Ziel zu definieren. |
| `MaxMarginUsage` | 30 | Obergrenze für den Margenverbrauch, ausgedrückt als Prozentsatz des Eigenkapitals. |
| `StopLossPips` | 50 | Die Stop-Loss-Größe wurde in MetaTrader Pips korrigiert, wenn ATR deaktiviert ist. |
| `UseAutoSl` | wahr | Ermöglicht ATR (Länge 14) Stop-Loss-Größe mit Multiplikator 1,5. |
| `CandleType` | 1-stündiger Zeitrahmen | Kerzenserien, die für Signale und die ATR-Berechnung verwendet werden. |

## Implementierungshinweise
- Die Strategie basiert auf StockSharp High-Level-Abonnements (`SubscribeCandles`) und Indikatorbindung (`AverageTrueRange`).
- Die Positionsgröße richtet sich nach dem Volumenschritt des Instruments sowie den minimalen und maximalen Volumenbeschränkungen.
- Margin-Prüfungen verwenden verfügbare Instrument-Margin-Hinweise (`MarginBuy`/`MarginSell`) wieder und greifen auf eine preisbasierte Schätzung zurück.
- Stop-Loss- und Take-Profit-Level werden intern durch die Überwachung von Kerzenhochs und -tiefs durchgesetzt, um ein einheitliches Verhalten aller Broker sicherzustellen.
- Alle Codekommentare sind gemäß den Konvertierungsrichtlinien auf Englisch.

## Dateien
- `CS/MpCandlestickStrategy.cs` – Hauptimplementierung der C#-Strategie.
- `README.md` – Englische Dokumentation (diese Datei).
- `README_zh.md` – Chinesische Übersetzung.
- `README_ru.md` – Russische Übersetzung.
