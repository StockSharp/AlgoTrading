# HTH Trader Absicherungs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine direkte Konvertierung des MetaTrader "HTH Trader" Expert Advisors. Sie handelt einen Vier-Bein-Forex-Korb und versucht, die tägliche Mean Reversion zwischen EURUSD und einem gespiegelten Korb aus USDCHF, GBPUSD und AUDUSD zu erfassen. Der StockSharp-Port behält die ursprünglichen Risikokontrollen und Zeitregeln bei und verwendet die High-Level-API für den Multi-Security-Handel.

Hauptmerkmale:

- Öffnet einen abgesicherten Korb einmal pro Tag zwischen 00:05 und 00:12 Terminalzeit.
- Verwendet die vorherigen zwei täglichen Schlusskurse von EURUSD, um die Korbrichtung zu bestimmen.
- Verwaltet vier Instrumente gleichzeitig: EURUSD (primäre Sicherheit), USDCHF, GBPUSD und AUDUSD.
- Verfolgt den offenen Gewinn in Pips und unterstützt korbweite Gewinn- und Verlustziele.
- Enthält eine Notfall-Verdoppelungsfunktion, die profitable Beine hinzufügt, wenn der Korb-Drawdown einen Schwellenwert überschreitet.
- Schließt alle Trades um 23:00 Terminalzeit oder wenn der Korb konfigurierte Gewinn-/Verlustgrenzen erreicht.

## Datenanforderungen

- **Intraday-Kerzen**: Alle vier Symbole müssen Intraday-Kerzen für den in `IntradayCandleType` konfigurierten Zeitrahmen liefern (Standard 5 Minuten). Diese Kerzen liefern den neuesten Preis und die Sessionsuhr.
- **Tageskerzen**: Jedes Symbol muss Tageskerzen bereitstellen, damit die Strategie die letzten zwei abgeschlossenen Tagesschlusskurse überwachen kann.

## Handelslogik

1. Am Ende jeder abgeschlossenen Intraday-Kerze prüft die Strategie den aktuellen offenen Gewinn:
   - Wenn `AllowEmergencyTrading` aktiviert ist und der gesamte offene Gewinn ≤ `-EmergencyLossPips`, verdoppelt die Strategie jedes Bein, das aktuell profitabel ist, und deaktiviert weitere Notfalltrades für diesen Tag.
   - Wenn `UseProfitTarget` aktiviert ist und der gesamte offene Gewinn ≥ `ProfitTargetPips`, wird der Korb sofort geschlossen.
   - Wenn `UseLossLimit` aktiviert ist und der gesamte offene Gewinn ≤ `-LossLimitPips`, wird der Korb sofort geschlossen.
2. Sobald die Uhr 23:00 erreicht, wird der Korb unabhängig vom Gewinn geschlossen.
3. Wenn keine Positionen offen sind und die Uhr im Fenster 00:05–00:12 liegt, prüft die Strategie die letzten zwei abgeschlossenen Tagesschlusskurse des primären Symbols (EURUSD standardmäßig):
   - Wenn die tägliche prozentuale Veränderung **positiv** ist, öffnet die Strategie: Long EURUSD, Long USDCHF, Short GBPUSD, Long AUDUSD.
   - Wenn die Veränderung **negativ** ist, öffnet sie: Short EURUSD, Short USDCHF, Long GBPUSD, Short AUDUSD.
   - Wenn die Veränderung null ist oder ein Tagesschlusskurs fehlt, überspringt die Strategie den Handel für diesen Tag.
4. Alle Positionen werden mit Marktorders über `ClosePosition` geschlossen.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeEnabled` | Aktiviert oder deaktiviert die Orderplatzierung. | `true` |
| `ShowProfitInfo` | Protokolliert den Korbgewinn in Pips bei jedem Update, während Positionen offen sind. | `true` |
| `UseProfitTarget` | Aktiviert das automatische Schließen, wenn `ProfitTargetPips` erreicht wird. | `false` |
| `UseLossLimit` | Aktiviert das automatische Schließen, wenn `LossLimitPips` erreicht wird. | `false` |
| `AllowEmergencyTrading` | Erlaubt die Notfall-Verdoppelungsfunktion. | `true` |
| `EmergencyLossPips` | Korb-Drawdown (in Pips), der die Notfallverdopplung auslöst. | `60` |
| `ProfitTargetPips` | Korbgewinn (in Pips), der das Schließen auslöst, wenn `UseProfitTarget` aktiviert ist. | `80` |
| `LossLimitPips` | Korbverlust (in Pips), der das Schließen auslöst, wenn `UseLossLimit` aktiviert ist. | `40` |
| `TradingVolume` | Ordervolumen für jedes Bein. | `0.01` |
| `Symbol2` | Zweite Sicherheit (USDCHF standardmäßig). | `null` |
| `Symbol3` | Dritte Sicherheit (GBPUSD standardmäßig). | `null` |
| `Symbol4` | Vierte Sicherheit (AUDUSD standardmäßig). | `null` |
| `IntradayCandleType` | Intraday-Zeitrahmen für Zeitplanung und Preisupdates. | `5`-Minuten-Kerzen |

## Verwendungshinweise

- Weisen Sie die primäre Sicherheit (`Strategy.Security`) EURUSD (oder dem gewünschten führenden Paar) zu und ordnen Sie `Symbol2`, `Symbol3`, `Symbol4` den korrelierten Instrumenten zu, bevor Sie starten.
- Stellen Sie sicher, dass jede Sicherheit einen gültigen `PriceStep` hat; andernfalls können Gewinnberechnungen in Pips nicht durchgeführt werden und die Notfalllogik bleibt inaktiv.
- Die Notfall-Verdoppelungsfunktion fügt nur Beinen hinzu, die aktuell profitabel sind; verlierende Beine werden unberührt gelassen, um den Drawdown nicht zu verstärken.
- Die Implementierung geht davon aus, dass Marktorders nahe dem letzten Kerzenschluss ausgeführt werden. Für eine genaue Buchführung verbinden Sie die Strategie mit einem Datenfeed, der zeitnahe Intraday-Kerzen liefert.
- Da die Logik durch eine einzelne Kerze pro Minute (oder gewähltem Zeitrahmen) gesteuert wird, kann das ursprüngliche Tick-für-Tick-MQL-Verhalten beim Ausführungszeitpunkt leicht abweichen, aber die Trade-Sequenzierung und Bedingungen stimmen mit dem Referenz-Expert-Advisor überein.
