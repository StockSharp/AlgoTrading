# Befehlsstrategie der ARD-Auftragsverwaltung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Strategieüberblick
Die **ARD Order Management**-Strategie portiert den MetaTrader 4-Expertenberater `ARD_ORDER_MANAGEMENT_.mq4` in das übergeordnete Strategie-Framework von StockSharp. Das ursprüngliche Skript stellte eine Reihe manueller Befehle bereit – Kaufen, Verkaufen, Schließen und Ändern –, die über externe Skripte oder UI-Schaltflächen ausgelöst werden konnten. Jeder Befehl berechnete das Handelsvolumen aus der verfügbaren freien Marge, geöffneten oder umgekehrten Marktpositionen neu und fügte schützende Stop-Loss- und Take-Profit-Levels in festen Punktabständen hinzu.

Die StockSharp-Version behält das gleiche Interaktionsmodell bei. Sie steuern das Verhalten durch den Parameter `Command`; Sobald ein Wert festgelegt wird, der nicht `None` ist, führt die Strategie die angeforderte Aktion bei der nächsten Aktualisierung der Ebene 1 aus und setzt den Befehl automatisch auf `None` zurück. Schutzaufträge werden bei jeder neuen Eingabe- oder Änderungsanforderung neu erstellt, sodass Stop-Loss und Take-Profit immer die aktuellen Parameterwerte widerspiegeln.

## Befehlslebenszyklus
1. **Befehlsversand** – wenn `Command` auf `Buy` oder `Sell` gesetzt ist, speichert die Strategie die Anfrage und ruft sofort `ClosePosition()` auf, um alle offenen Exposures zu reduzieren. Aktive Schutzaufträge werden storniert, bevor der neue Handel berücksichtigt wird, was die MQL-Schleife widerspiegelt, die alle Tickets über `OrderClose` geschlossen hat.
2. **Volumenberechnung** – die Lautstärke wird für jeden Befehl neu berechnet. Es verwendet `Portfolio.CurrentValue` (Fallback auf `Portfolio.BeginValue`), geteilt durch `LotSizeDivisor` und skaliert durch `1/1000`, genau so, wie `AccountFreeMargin()/lotsize/1000` in MetaTrader verwendet wurde. Das Ergebnis wird auf `LotDecimals` gerundet und durch `MinimumVolume` eingeschränkt.
3. **Warten auf eine flache Position** – wenn eine Position offen war, als der Befehl eintraf, wird der neue Eintrag zurückgestellt, bis `Position` auf Null fällt. Die Strategie überprüft diese Bedingung bei jedem Tick der Stufe 1, um ein Überlaufen der asynchronen Ausführungspipeline zu vermeiden.
4. **Marktausführung** – Sobald die Strategie flach ist, sendet sie `BuyMarket` oder `SellMarket`. Die letzten bekannten besten Geld-/Briefkurse werden gespeichert, sodass Schutzaufträge aus realistischen Ausführungspreisen abgeleitet werden können.
5. **Schutzplatzierung** – Stop-Loss- und Take-Profit-Level werden als separate Stop- und Limit-Orders realisiert. Bei Long-Trades liegt der Stop bei `bid − StopLossPoints * PriceStep` und das Ziel bei `ask + TakeProfitPoints * PriceStep`. Short-Trades kehren diese Berechnungen um. Änderungsbefehle verwenden dieselbe Routine wieder, jedoch mit `ModifyStopLossPoints` und `ModifyTakeProfitPoints`.
6. **Befehl schließen** – Durch Setzen von `Command` auf `Close` werden alle Schutzbefehle aufgehoben und `ClosePosition()` aufgerufen. Wenn die Strategie bereits flach ist, protokolliert der Befehl einfach die Tatsache und unternimmt nichts anderes.

## Geldmanagement
- **Margengesteuertes Volumen** – der Code prüft den aktuellen Portfoliowert, sodass das Volumen mit dem verfügbaren Kapital schrumpft oder wächst. Wenn der Divisor-Parameter versehentlich auf Null fällt, greift der Algorithmus auf den konfigurierten `MinimumVolume` zurück und gibt eine Warnung aus.
- **Rundung** – `LotDecimals` definiert, wie viele Dezimalstellen nach dem Runden erhalten bleiben. Die Implementierung verwendet `Math.Round` mit `MidpointRounding.AwayFromZero`, sodass sich positive und negative Anpassungen wie MetaTraders `NormalizeDouble` verhalten.
- **Mindestlos** – nach dem Runden wird die Größe mit `MinimumVolume` begrenzt, wodurch das ursprüngliche Verhalten reproduziert wird, bei dem Werte unter `lotmax` auf `0.1` heraufgestuft wurden.

## Stop-Loss- und Take-Profit-Handhabung
- Schutzbefehle werden immer von Grund auf neu erstellt. Bestehende Stop- oder Take-Orders werden storniert, bevor neue aufgegeben werden.
- Die Strategie prüft `Security.PriceStep`, bevor absolute Preise berechnet werden. Wenn der Schritt fehlt oder nicht positiv ist, werden Schutzanordnungen übersprungen und eine Warnung protokolliert.
- Änderungsbefehle (`Command = Modify`) stellen den Schutz unter Verwendung der dedizierten Änderungsabstände wieder her, ohne die aktuelle Positionsgröße zu ändern.

## Daten- und Ausführungsanforderungen
- Abonniert Daten der Ebene 1 über `SubscribeLevel1()`, um die MetaTrader-Angebotsaktualisierungen (`Bid`/`Ask`) widerzuspiegeln.
- Erfordert keine Kerzen oder Indikatoren; Die gesamte Logik wird bei Tick-/Kursaktualisierungen ausgeführt.
- Verwendet High-Level-Helfer (`BuyMarket`, `SellMarket`, `BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`, `CancelOrder`), um innerhalb der von StockSharp empfohlenen API-Ebene zu bleiben.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `SlippageSteps` | int | 4 | Zulässiger Slippage ausgedrückt in Preisschritten. Aus Kompatibilitätsgründen gespeichert; StockSharp Marktaufträge werden sofort ausgeführt und verbrauchen diesen Wert nicht. |
| `LotDecimals` | int | 1 | Anzahl der Dezimalstellen, die nach dem Runden des berechneten Volumens beibehalten werden. |
| `StopLossPoints` | dezimal | 50 | Abstand (in Preispunkten) vom Einstieg bis zum anfänglichen Stop-Loss. |
| `TakeProfitPoints` | dezimal | 100 | Abstand (in Preispunkten) vom Einstieg bis zum anfänglichen Take-Profit. |
| `LotSizeDivisor` | dezimal | 5 | Teilt den Portfoliowert vor der Skalierung in Lots (`freeMargin / divisor / 1000`). |
| `ModifyStopLossPoints` | dezimal | 20 | Stop-Loss-Distanz angewendet, wenn `Command = Modify`. |
| `ModifyTakeProfitPoints` | dezimal | 100 | Take-Profit-Distanz angewendet, wenn `Command = Modify`. |
| `MinimumVolume` | dezimal | 0,1 | Untergrenze für das Endvolumen nach Rundung. |
| `OrderComment` | Zeichenfolge | `"Placing Order"` | Zur einfacheren Prüfung wird in jede Bestellung ein Kommentar eingefügt. |
| `Command` | `ArdOrderCommand` | `None` | Manueller Befehl zur Ausführung. Nach der Verarbeitung automatisch auf `None` zurückgesetzt. |

## Nutzungshinweise
- Legen Sie `Command` über die Benutzeroberfläche oder programmgesteuert fest. Der Befehl wird pro Änderung nur einmal verarbeitet; Um eine Aktion zu wiederholen, setzen Sie sie wieder auf `None` und dann wieder auf den gewünschten Wert.
- Da Stop-Loss und Take-Profit als unabhängige Orders platziert werden, müssen Broker/Börsen native Stop-/Limit-Orders für dasselbe Wertpapier unterstützen. Ist dies nicht der Fall, sollten Sie erwägen, sie durch synthetische Exits im Code zu ersetzen.
- Slippage wird als Parameter für die Dokumentationsparität mit der MT4-Version beibehalten. Die High-Level-Markthelfer von StockSharp stellen keinen expliziten Slippage-Parameter zur Verfügung, daher dient der Wert nur der Information.
- Die Strategie protokolliert jede wichtige Aktion (`LogInfo`/`LogWarn`), um Audit-Trails während der diskretionären Ausführung zu unterstützen.

## Unterschiede im Vergleich zum ursprünglichen MQL Expert Advisor
- MetaTrader hat Stopps und Ziele direkt an das Marktticket angehängt. StockSharp gibt stattdessen separate Stop- und Limit-Orders aus.
- Der Port verwendet das asynchrone Ereignismodell von StockSharp. Beim Stornieren einer Position wartet der Eintrag, bis die vorherige Position als geschlossen gemeldet wird, um eine Überlappung der Aufträge zu verhindern.
- Portfolioinformationen ersetzen `AccountFreeMargin`. Stellen Sie sicher, dass der Portfolio-Adapter `CurrentValue` ausfüllt oder konfigurieren Sie `BeginValue`, bevor Sie mit der Strategie beginnen.
- Die Fehlerbehandlung basiert auf der StockSharp-Protokollierung und nicht auf wiederholten `OrderSend`-Wiederholungsversuchen, da Ausnahmen bei der Auftragsübermittlung vom Framework selbst angezeigt werden.

## Testtipps
- Führen Sie die Strategie in einer Simulation mit Level-1-Daten aus, um zu bestätigen, dass Schutzbefehle in den erwarteten Entfernungen erscheinen.
- Experimentieren Sie mit verschiedenen `LotSizeDivisor`- und `LotDecimals`-Werten, um sie an die Vertragsspezifikationen des Brokers anzupassen, bevor Sie die Strategie in Live-Umgebungen verwenden.
