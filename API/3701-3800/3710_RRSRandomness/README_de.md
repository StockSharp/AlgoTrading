# RRS-Zufallsstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die **RRS Randomness Strategy** ist eine StockSharp-Portierung von „RRS Randomness in Nature EA“ für MetaTrader 4.
Es emuliert den ursprünglichen Expertenberater, indem es zufällige Long- oder Short-Markteintritte generiert, Stop-Loss- und Take-Profit-Level anwendet, optional profitable Trades nachverfolgt und eine risikobasierte Liquidation durchführt, wenn variable Verluste den konfigurierten Schwellenwert überschreiten.

Da StockSharp Nettopositionen pro Wertpapier verwendet, wird ein gleichzeitiges Long- und Short-Engagement nicht unterstützt. Der „DoubleSide“-Modus wechselt daher die Einstiegsrichtung bei jeder Gelegenheit, anstatt wie in MetaTrader zwei abgesicherte Geschäfte aufrechtzuerhalten.

## Handelslogik

1. Bei jeder fertigen Kerze bewertet die Strategie den neuesten Marktpreis, der aus Trades oder Level-1-Notierungen ermittelt wurde.
2. Wenn eine offene Position vorliegt, werden Stop-Loss-, Take-Profit- und Trailing-Stop-Regeln durchgesetzt und eine Portfolio-Risikoprüfung durchgeführt.
3. Wenn es flach ist, werden Spread- und Volumenbeschränkungen überprüft, bevor ein neuer Handel eröffnet wird:
   - Der **DoubleSide**-Modus wechselt zwischen langen und kurzen Einträgen.
   - Der **OneSide**-Modus folgt der ursprünglichen EA-Regel: Eine zufällige Ganzzahl in `[0,5]` öffnet Long-Werte für die Werte `1` oder `4` und Short-Werte für `0` oder `3`.
4. Handelsvolumina werden einheitlich zwischen dem konfigurierten Minimum und Maximum gezogen und an der Instrumentenvolumenstufe ausgerichtet.

## Parameter

| Gruppe | Name | Beschreibung |
|-------|------|-------------|
| Allgemein | `Mode` | Handelsmodus: alternative Einträge (`DoubleSide`) oder zufällige Eingaben (`OneSide`). |
| Grundstückseinstellungen | `MinVolume` / `MaxVolume` | Volumenbereich für zufällig generierte Trades. |
| Schutz | `TakeProfitPoints` | Take-Profit-Distanz in Preisschritten. |
| Schutz | `StopLossPoints` | Stop-Loss-Distanz in Preisschritten. |
| Schutz | `TrailingStartPoints` | Gewinndistanz, die ein Trailing-Stop-Management ermöglicht. |
| Schutz | `TrailingGapPoints` | Offset zwischen Marktpreis und Trailing Stop. |
| Filter | `MaxSpreadPoints` | Maximal zulässiger Spread (in Preisschritten) für die Eröffnung neuer Positionen. |
| Filter | `SlippagePoints` | Informative Slippage-Einstellung (nicht automatisch erzwungen). |
| Risikomanagement | `MoneyRiskMode` | Wählen Sie zwischen einem festen Währungsverlust oder einem Prozentsatz des Portfoliowerts. |
| Risikomanagement | `RiskValue` | Höhe des Risikos (Währung oder Prozentsatz je nach Modus). |
| Allgemein | `TradeComment` | Informationskommentar an generierte Bestellungen angehängt. |
| Allgemein | `CandleType` | Kerzenserien treiben die Entscheidungsschleife voran. |

## Notizen

- Die Strategie basiert auf Marktdatenabonnements für Kerzen, Level1-Kurse und Trades. Stellen Sie sicher, dass der ausgewählte Datentyp für die ausgewählte Sicherheit verfügbar ist.
- Die Trailing-Stop-Logik spiegelt die MQL-Implementierung wider: Sie wird aktiviert, nachdem der Preis um `TrailingStartPoints + TrailingGapPoints` Schritte gestiegen ist, und folgt dann dem Preis in einem Abstand von `TrailingGapPoints`.
- Das Risikomanagement vergleicht den variablen PnL mit dem konfigurierten Verlustschwellenwert und liquidiert die Position, wenn der Schwellenwert überschritten wird.
