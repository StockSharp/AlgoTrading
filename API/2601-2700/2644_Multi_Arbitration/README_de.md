# Multi Arbitrage Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Multi Arbitrage Strategie** ist ein StockSharp-Port des MetaTrader-Expertenberaters "Multi_arbitration 1.000". Das ursprüngliche Skript bewertet kontinuierlich bestehende Kauf- und Verkaufspositionen, fügt neue Trades in der Richtung mit schwächerem schwebenden Gewinn hinzu, und führt eine globale Liquidation durch, sobald die Gesamtgewinnziele erreicht sind. Diese C#-Implementierung behält die Kernentscheidungslogik bei und passt sie an StockSharp's Netting-Portfolio-Modell und die High-Level-Strategie-API an.

Die Strategie:
- Öffnet eine anfängliche Long-Position, sobald die erste abgeschlossene Kerze eintrifft.
- Vergleicht den nicht realisierten Gewinn der aktiven Richtung mit der alternativen Richtung, um zu entscheiden, ob ein Richtungswechsel erforderlich ist.
- Erzwingt eine flache Position, wenn das konfigurierte Gewinnziel überschritten wird oder wenn der Positionsdruck über eine konfigurierbare Grenze hinauswächst.
- Verwendet nur Market-Orders (`BuyMarket` / `SellMarket`) für Einfachheit und schnelle Ausführung.

## Handelslogik
1. **Anfangsorder** – Die allererste abgeschlossene Kerze löst eine Long-Market-Order mit dem konfigurierten Handelsvolumen aus. Dies reproduziert den sofortigen Markteinstieg des MetaTrader-Expertenberaters.
2. **Gewinnvergleich** – Bei jeder abgeschlossenen Kerze berechnet die Strategie den schwebenden PnL der aktuellen Richtung:
   - Long-Gewinn = `(close - entry) * volume`
   - Short-Gewinn = `(entry - close) * volume`
3. **Positionsauswahl** – Wenn die alternative Richtung derzeit besser als die aktive abschneiden würde, dreht die Strategie die Position um, indem sie eine Market-Order sendet, die so dimensioniert ist, dass sie das bestehende Engagement abdeckt und eine neue Position in die neue Richtung eröffnet. Wenn keine Position offen ist, wählt der Algorithmus standardmäßig einen Long-Einstieg, was dem ursprünglichen Expertenberater entspricht.
4. **Positionslimitschutz** – Ein konfigurierbarer `MaxOpenPositions`-Parameter spiegelt die MetaTrader-Prüfung gegen `LimitOrders()` wider. Wenn das kombinierte Long/Short-Engagement diesen Grenzwert erreicht und die Strategie profitabel ist, flacht sie das Buch ab, um Überhebelung zu vermeiden.
5. **Gewinnzielausstieg** – Wenn der Konto-PnL (realisiert + nicht realisiert) den `ProfitForClose`-Schwellenwert überschreitet, schließt die Strategie alle Positionen, genau wie die ursprüngliche `Equity - Balance`-Prüfung.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `TradeVolume` | Für jede Market-Order verwendetes Volumen. Entspricht der Mindest-Lotgröße im Original-EA. | `1` |
| `ProfitForClose` | Gewinnschwelle, die einen globalen Ausstieg auslöst, sobald sie überschritten wird. | `300` |
| `MaxOpenPositions` | Maximale Anzahl gleichzeitiger Positionen, bevor die Strategie eine Abflachung erzwingt. Entspricht `limit - 15`. | `15` |
| `CandleType` | Kerzen-Datentyp zur Synchronisierung von Handelsentscheidungen. Standard ist 1-Minuten-Zeitrahmen. | `1-Minuten-Kerzen` |

## Implementierungshinweise
- StockSharp verwendet ein Netting-Positionsmodell, daher kann die Strategie nur eine Netto-Richtung gleichzeitig halten. Richtungswechsel werden durch Dimensionierung von Market-Orders behandelt, die sowohl das bestehende Engagement schließen als auch eine neue Position in die entgegengesetzte Richtung eröffnen.
- Der `StartProtection()`-Aufruf wird verwendet, um integriertes Risiko-Handling zu erben (z.B. Stop-out bei Nicht-Null-Positionen, wenn die Strategie gestoppt wird).
- Alle Zustandsvariablen (`_entryPrice`, `_currentSide`, `_initialOrderPlaced`) werden bei `OnReseted` zurückgesetzt, um Neustarts und wiederholte Simulationen ohne veraltete Daten zu unterstützen.
- Die Strategie reagiert nur auf **abgeschlossene Kerzen**, um Doppelzählungen von Gewinnen auf teilweise gebildeten Balken zu vermeiden.

## Nutzungsempfehlungen
- Richten Sie den `TradeVolume`-Parameter an der Lotgröße des Instruments oder dem Kontraktmultiplikator aus.
- Der `ProfitForClose`-Wert sollte in derselben Währung wie der Konto-PnL festgelegt werden (z.B. USD für FX-Konten).
- Erhöhen oder verringern Sie `MaxOpenPositions` je nachdem, wie aggressiv die Strategie Engagement aufbauen soll, bevor eine Abflachung erzwungen wird.
- Da die Strategie immer mit einem Long-Trade beginnt, sollten Sie sie manuell starten, wenn Long-Einstiege für das gehandelte Instrument akzeptabel sind.

## Unterschiede zur MetaTrader-Version
- MetaTrader's Hedging-Modus erlaubt gleichzeitige Long- und Short-Positionen, während dieser Port in einer Netting-Umgebung arbeitet. Die Entscheidungslogik vergleicht immer noch die Richtungsrentabilität, aber nur eine Netto-Position wird jederzeit gehalten.
- Plattformspezifische Prüfungen (Terminal-Handelsberechtigungen, Auswahl des Füllungstyps, Konto-Magic-Nummern) werden durch StockSharp-Äquivalente wie `StartProtection()` und Kerzenabonnements ersetzt.
- Kommentierte Diagnosen aus der MQL-Datei werden nicht reproduziert; verlassen Sie sich auf StockSharp-Logging, wenn Laufzeitinformationen benötigt werden.
