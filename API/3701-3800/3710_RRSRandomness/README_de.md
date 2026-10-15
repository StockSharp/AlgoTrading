# RRS-Zufallsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **RRS Randomness Strategy** ist eine StockSharp-Portierung von „RRS Randomness in Nature EA“ für MetaTrader 4.
Sie emuliert den ursprünglichen Expert Advisor mit pseudozufälligen Long- oder Short-Markteinstiegen, Stop-Loss- und Take-Profit-Prüfungen auf abgeschlossenen Kerzen, optionalem Trailing und einer Liquidation beim Erreichen der Verlustschwelle.

Da StockSharp Nettopositionen pro Wertpapier verwendet, wird ein gleichzeitiges Long- und Short-Engagement nicht unterstützt. `DoubleSide` beginnt daher mit Long und wechselt nach jedem Einstieg die Richtung, statt wie in MetaTrader zwei abgesicherte Geschäfte zu halten.

## Handelslogik

1. Bei jeder abgeschlossenen Kerze nutzt die Strategie den Schlusskurs für Schutzprüfungen und verfügbare Level-1-Bid/Ask-Kurse für Spread und Liquidationspreis.
2. Bei offener Position prüft sie Stop-Loss, Take-Profit, Trailing-Stop und das Verlustlimit; pro Kerze wird höchstens eine Schließungsorder gesendet.
3. Wenn es flach ist, werden Spread- und Volumenbeschränkungen überprüft, bevor ein neuer Handel eröffnet wird:
   - **DoubleSide** wechselt zwischen Long und Short und beginnt mit Long.
   - **OneSide** nutzt eine wiederholbare pseudozufällige Ganzzahl in `[0,5]`: `1` oder `4` eröffnet Long, `0` oder `3` Short, `2` oder `5` überspringt die Kerze. Beim Start oder Reset beginnt die Folge neu.
4. Handelsvolumina werden einheitlich zwischen dem konfigurierten Minimum und Maximum gezogen und an der Instrumentenvolumenstufe ausgerichtet.

## Parameter

| Gruppe | Name | Beschreibung |
|-------|------|-------------|
| Allgemein | `Mode` | Wechselnde Einstiege (`DoubleSide`, `0`) oder zufällig gefilterte Einstiege (`OneSide`, `1`). |
| Grundstückseinstellungen | `MinVolume` / `MaxVolume` | Volumenbereich für zufällig generierte Trades. |
| Schutz | `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten. |
| Schutz | `StopLossPoints` | Stop-Loss-Distanz in Preisschritten. |
| Schutz | `TrailingStartPoints` | Gewinndistanz, die ein Trailing-Stop-Management ermöglicht. |
| Schutz | `TrailingGapPoints` | Offset zwischen Marktpreis und Trailing Stop. |
| Filter | `MaxSpreadPoints` | Maximaler Level-1-Spread in Preisschritten. Null sperrt neue Einstiege; ein positiver Wert erlaubt den Kerzen-Fallback ohne Bid/Ask. |
| Filter | `SlippagePoints` | Informative Slippage-Einstellung (nicht automatisch erzwungen). |
| Risikomanagement | `MoneyRiskMode` | Fester Geldverlust (`FixedMoney`, `0`) oder Portfoliowert in Prozent (`BalancePercentage`, `1`). |
| Risikomanagement | `RiskValue` | Höhe des Risikos (Währung oder Prozentsatz je nach Modus). |
| Allgemein | `TradeComment` | Kommentar für Einstiegsorders; Schließungsorders ergänzen den Auslöser. |
| Allgemein | `CandleType` | Kerzenserien treiben die Entscheidungsschleife voran. |

## Notizen

- Level-1-Kurse verbessern die Berechnung von Spread und Liquidationspreis. Fehlen beide Seiten, erlaubt ein positiver Grenzwert den Kerzen-Fallback; `MaxSpreadPoints = 0` sperrt Einstiege immer.
- Schutzregeln werden auf abgeschlossenen Kerzen geprüft. Trailing startet nach `TrailingStartPoints + TrailingGapPoints` Schritten Gewinn und folgt mit `TrailingGapPoints` Abstand.
- `FixedMoney` interpretiert `RiskValue` in Kontowährung; `BalancePercentage` verwendet diesen Prozentsatz des aktuellen Portfoliowerts.
