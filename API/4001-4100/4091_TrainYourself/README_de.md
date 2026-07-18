# TrainYourself-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters **TrainYourself-V1_1-1**. Es stellt die Kanalaufbau- und Breakout-Logik wieder her und ersetzt gleichzeitig die grafischen Schaltflächen des MT4-Skripts durch explizite Methodenaufrufe. Der Algorithmus baut kontinuierlich einen Preiskanal im Donchian-Stil neu auf und löst einen Handel aus, sobald der Preis den Kanal verlässt, nachdem er sich zunächst darin konsolidiert hat.

## Handelslogik

1. **Kanalbau**
   - Ein `DonchianChannels`-Indikator mit `ChannelLength` Perioden wird für jede abgeschlossene Kerze des ausgewählten `CandleType` ausgewertet.
   - Die rohen oberen und unteren Bänder werden mit einem zusätzlichen MetaTrader-ähnlichen Puffer erweitert: `BufferPoints` multipliziert mit dem Instrument `PriceStep`. Dies reproduziert das ursprüngliche Skript, bei dem die Trendlinien zunächst 50 Punkte vom aktuellen Geld-/Briefkurs entfernt platziert wurden, bevor sie über die jüngsten Hochs und Tiefs gleiten.
   - Die resultierenden `UpperBand`/`LowerBand`-Werte werden als schreibgeschützte Eigenschaften bereitgestellt, sodass sie in benutzerdefinierten Dashboards angezeigt werden können.

2. **Scharfschaltzustand**
   - Die Breakout-Engine bleibt deaktiviert, während eine Position offen ist oder wenn `EnableTrendTrade` falsch ist.
   - Wenn keine Position vorhanden ist, muss der Preis innerhalb des Kanals mit einer zusätzlichen Marge von `ActivationPoints` * `PriceStep` von beiden Grenzen schließen. Erst dann wird `_isArmed` zu `true` und ahmt die MetaTrader-Flagge `q=1` nach, die gesetzt wurde, als der Preis wieder in den Kanal zurückging.

3. **Breakout-Ausführung**
   - Sobald der Kurs aktiviert ist, wird bei einem Schlusskurs bei oder über `UpperBand` eine Marktkauforder platziert (sofern `AllowBuyOpen` aktiviert ist). Ein Schlusskurs bei oder unter `LowerBand` platziert einen Marktverkaufsauftrag (in Bezug auf `AllowSellOpen`).
   - Nachdem eine Order aufgegeben wurde, entschärft sich die Strategie, bis der Preis ohne offene Positionen wieder in den Kanal eintritt.

4. **Risikomanagement**
   - `StartProtection` konfiguriert automatische Schutzanordnungen. Entfernungen werden durch Multiplikation von `TakeProfitPoints` und `StopLossPoints` mit dem aktuellen `PriceStep` berechnet. Wenn der Broker einen Schritt nicht meldet, wird ein Fallback von `0.0001` verwendet, der dem Verhalten von MetaTrader `Point` entspricht.

5. **Manuelle Steuerung**
   - Die MT4-Labels (`BUY_TRIANGLE`, `SELL_TRIANGLE`, `CLOSE_ORDER`) werden durch drei öffentliche Methoden ersetzt: `TriggerManualBuy()`, `TriggerManualSell()` und `ClosePositionManually()`. Sie respektieren `AllowBuyOpen`/`AllowSellOpen`, überprüfen den Verbindungsstatus über `IsFormedAndOnlineAndAllowTrading()` und deaktivieren außerdem die Breakout-Logik, sodass manuelle Trades nicht sofort automatische Einträge auslösen.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | `30m` Zeitrahmen | Primäres Kerzenabonnement, das für alle Berechnungen verwendet wird. |
| `ChannelLength` | `20` | Anzahl der vom Kanal Donchian analysierten Kerzen. |
| `BufferPoints` | `50` | Zusätzliche MetaTrader Punkte wurden rund um den letzten Schlusskurs vor Abschluss des Kanals hinzugefügt. |
| `ActivationPoints` | `2` | Marge (in Punkten), die der Preis von den Kanalrändern fernhalten muss, bevor ein Ausbruch ausgelöst werden kann. |
| `StopLossPoints` | `100` | Stop-Loss-Distanz in Punkten; durch Multiplikation mit `PriceStep` in den absoluten Preis umgerechnet. |
| `TakeProfitPoints` | `100` | Take-Profit-Distanz in Punkten; mit `PriceStep` in absoluten Preis umgerechnet. |
| `EnableTrendTrade` | `true` | Ermöglicht den automatischen Breakout-Handel. Bei `false` können nur die manuellen Hilfsmethoden Positionen öffnen/schließen. |
| `Volume` | `1` | Ordergröße für automatische und manuelle Trades. |

## Nutzungshinweise

- Der ursprüngliche Expert Advisor erforderte das Ziehen von Symbolen auf dem Chart, um Trendlinien (neu) aufzubauen. In StockSharp wird der Kanal bei jeder Kerze automatisch neu aufgebaut, sodass keine manuelle Aktualisierung erforderlich ist.
- Da die Strategie `UpperBand`, `LowerBand` und `IsArmed` verfügbar macht, können Dashboards oder UI-Widgets das ursprüngliche visuelle Feedback reproduzieren, ohne auf Diagrammobjekte angewiesen zu sein.
- Stop-Loss- und Take-Profit-Level sind optional. Setzen Sie die entsprechenden Parameter auf `0`, um die Schutzanordnungen zu deaktivieren. Dies spiegelt das Verhalten von MetaTrader wider, bei dem die Änderungsroutinen übersprungen wurden, wenn der externe Wert Null war.
- Manuelle Eingaben berücksichtigen denselben `Volume`-Parameter und profitieren automatisch von den konfigurierten Schutzabständen.
- Um den Ausbruchsstatus manuell zurückzusetzen, rufen Sie `ClosePositionManually()` auf (wodurch auch `IsArmed` gelöscht wird) oder warten Sie, bis der Preis wieder in den Kanal eintritt, damit die Aktivierungsbedingung wieder erfüllt ist.
