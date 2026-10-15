# Peter Panel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Peter Panel-Strategie** portiert das diskretionäre MetaTrader 5-Kontrollfeld „Peter Panel“ in StockSharp. Der ursprüngliche Fachberater zeichnete drei horizontale Linien (Einstieg, Take-Profit und Stop-Loss) und eine Schaltflächenmatrix, die es dem Händler ermöglichte, anhand dieser Ebenen sofort Markt- oder Pending-Orders einzureichen. Diese C#-Strategie hält den Entscheidungsfluss intakt und ersetzt gleichzeitig das grafische Bedienfeld durch interaktive Strategieparameter. Jeder Umschalter verhält sich wie die ursprüngliche Schaltfläche: Wenn Sie den Parameter auf `true` setzen, wird die Aktion sofort ausgeführt und das Flag wird wieder auf `false` zurückgesetzt.

## Schlüsselkonzepte

1. **Manueller Assistent** – die Strategie generiert keine Signale. Sie entscheiden, wann Sie handeln möchten, indem Sie die in der Strategie-Benutzeroberfläche oder in Automatisierungsskripten angezeigten Parameter umschalten.
2. **Gemeinsame Preislinien** – die Aqua-Einstiegslinie, die grüne Take-Profit-Linie und die rote Stop-Loss-Linie werden durch drei Dezimalparameter dargestellt. Ihre Werte können manuell eingestellt oder über den Schalter `ResetCommand` um den aktuellen Mittelpreis herum neu berechnet werden.
3. **Umfassende Orderabdeckung** – alle sechs Ordertypen aus dem Panel sind implementiert: Marktkauf/-verkauf, Kaufstopp, Kauflimit, Verkaufsstopp und Verkaufslimit. Nach jedem Ausfüllen werden Schutzbefehle angehängt, die die TP/SL-Felder emulieren, die das Panel MetaTrader automatisch ausfüllt.
4. **Massenänderungen** – der Parameter `ModifyCommand` wendet die aktuellen Preislinien erneut auf jede aktive ausstehende Order und auf die schützenden Stop-Loss-/Take-Profit-Orders der offenen Position an.
5. **One-Touch-Liquidation** – Die Schaltfläche `CloseCommand` storniert ausstehende ausstehende Aufträge, entfernt Schutzaufträge und glättet die Nettoposition auf dem Markt.

## Ursprüngliche vs. StockSharp-Implementierung

| Funktion | MetaTrader 5 Peter Panel | StockSharp Peter Panel-Strategie |
| --- | --- | --- |
| Benutzeroberfläche | Dialog auf dem Diagramm mit Schaltflächen und bearbeitbaren Feldern | Strategieparameter, die sich wie Schalter und numerische Eingaben verhalten |
| Eingabe-/TP-/SL-Manipulation | Ziehen Sie horizontale Linien oder drücken Sie „Zurücksetzen“, um sie neu zu zentrieren | Bearbeiten Sie Parameterwerte direkt oder verwenden Sie den Schalter `ResetCommand` |
| Auftragserteilung | Die Schaltfläche löst eine synchrone `OrderSend`-Anfrage aus | Parameter toggle ruft den entsprechenden `Buy/Sell`-Helper auf und speichert Bestellreferenzen |
| TP/SL-Handhabung | Wird in jeder Bestellung durch `MqlTradeRequest.tp` und `.sl` ausgefüllt | Schutzstopp und Ziel werden unmittelbar nach der Ausführung als separate Stop-/Limit-Orders registriert |
| Auftragsänderung | Wählen Sie ein Ticket in der Liste aus und klicken Sie auf „Ändern“. | `ModifyCommand` storniert/ersetzt jede aktive ausstehende Bestellung und aktualisiert Schutzbestellungen |
| Auftragsabschluss | Drücken Sie auf dem markierten Ticket auf „Schließen“. | `CloseCommand` schließt die gesamte Position und storniert alle ausstehenden und Schutzaufträge |
| Bestellliste | Grafische Tabelle mit Tickets und Stufen | Die Strategie basiert auf der Auftragsverfolgung von StockSharp; Der detaillierte Status ist in den Protokollen verfügbar |

> **Hinweis:** MetaTrader erlaubte dem Händler, ein einzelnes Ticket aus der Liste auszuwählen. Der StockSharp-Port wendet Änderungen und Schließungen auf jede von der Strategie erstellte Bestellung an, da in den Strategieparametern keine direkte Einzelticketauswahl verfügbar ist.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `Volume` | Handelsvolumen in Lots. Es wird anhand des Sicherheitsvolumenschritts und der Min/Max-Grenzwerte validiert. |
| `EntryLevel` | Preis für ausstehende Orders (Aqua-Linie). |
| `TakeProfitLevel` | Preis der grünen Linie. Es fungiert als Take-Profit-Level für Long-Trades und als schützendes Stop-Level für Short-Trades und spiegelt das ursprüngliche Panel wider. |
| `StopLossLevel` | Preis der roten Linie. Es fungiert als schützender Stopp für Long-Trades und als Take-Profit-Ziel für Short-Trades. |
| `BuyMarketCommand` | Senden Sie eine Marktkauforder, wenn diese auf `true` eingestellt ist. Das Flag wird auf `false` zurückgesetzt, nachdem die Bestellung gesendet wurde. |
| `BuyStopCommand` | Geben Sie eine Kauf-Stopp-Order bei `EntryLevel` auf. |
| `BuyLimitCommand` | Geben Sie eine Kauflimitbestellung bei `EntryLevel` auf. |
| `SellMarketCommand` | Senden Sie einen Marktverkaufsauftrag. |
| `SellStopCommand` | Geben Sie eine Verkaufsstopp-Order bei `EntryLevel` auf. |
| `SellLimitCommand` | Geben Sie einen Verkaufslimitauftrag bei `EntryLevel` auf. |
| `ModifyCommand` | Wenden Sie `EntryLevel`, `TakeProfitLevel` und `StopLossLevel` erneut auf bestehende ausstehende Anordnungen und auf die Schutzanordnungen der aktuellen Position an. |
| `CloseCommand` | Stornieren Sie ausstehende Aufträge, entfernen Sie Schutzaufträge und glätten Sie die Marktposition. |
| `ResetCommand` | Berechnen Sie die drei Preisniveaus um den aktuellen Geld-/Briefmittelpunkt herum neu. |

## Arbeitsablauf

1. Starten Sie die Strategie, sobald das gewünschte Wertpapier und Portfolio verbunden sind. Das Abonnement der Stufe 1 aktualisiert den internen Bid/Ask-Cache, der die Funktion `ResetCommand` unterstützt.
2. Verwenden Sie den Schalter `ResetCommand` oder manuelle Änderungen, um die Preisstufen Aqua, Grün und Rot zu konfigurieren.
3. Lösen Sie einen Handel aus, indem Sie einen der Aktionsparameter auf `true` umschalten. Die Strategie setzt den Schalter automatisch auf `false` zurück, sodass die nächste Aktivierung beabsichtigt ist.
4. Nach der Ausführung übermittelt die Strategie die entsprechenden Stop-Loss- und Take-Profit-Orders basierend auf der Richtung der Position. Beispielsweise erhält eine Long-Position einen Verkaufsstopp an der roten Linie und ein Verkaufslimit an der grünen Linie, während eine Short-Position die umgekehrte Kombination erhält.
5. Ändern Sie die Ebenen jederzeit und drücken Sie `ModifyCommand`, um ausstehende Aufträge und Schutzausstiege zu aktualisieren, ohne die Strategie neu zu starten.
6. Wenn die Handelssitzung beendet ist, schalten Sie `CloseCommand` um, um alle von der Strategie verwalteten Aufträge zu reduzieren und zu bereinigen.

## Unterschiede zum Originalpanel

- Es gibt keine grafische Liste der Tickets. Stattdessen verfolgen StockSharp-Protokolle jede registrierte Bestellung und jeden registrierten Handel. Sie können die Strategie mit jeder externen Benutzeroberfläche verbinden, wenn eine individuelle Ticketverwaltung erforderlich ist.
- Stop-Loss- und Take-Profit-Werte werden als explizite untergeordnete Orders implementiert, da StockSharp TP/SL-Preise nicht direkt in die Hauptorderanfrage einbetten kann. Das Verhalten stimmt mit dem Endergebnis des Panels MetaTrader überein: Die Position wird durch die gleichen Ebenen geschützt.
- Das Ersetzen von Bestellungen erfolgt über Abbruch- und Neuerstellungszyklen. Dadurch bleibt der Workflow auch an Veranstaltungsorten deterministisch, die keine direkten Änderungen unterstützen.

## Nutzungstipps

- Kombinieren Sie die Strategie mit StockSharp-Diagrammen oder Dashboards, um das ursprüngliche Panel-Erlebnis wiederherzustellen, indem Sie die Schaltflächen auf dem Diagramm durch UI-Elemente ersetzen, die die angezeigten Parameter umschalten.
- Die Strategie stellt nicht mehrere Aktionen in die Warteschlange. Wenn Sie Abläufe automatisieren müssen (z. B. Ebenen zurücksetzen und dann eine ausstehende Bestellung aufgeben), lösen Sie die Umschaltungen nacheinander aus, nachdem die vorherige auf `false` zurückgesetzt wurde.
- Schutzaufträge werden nur für Positionen ungleich Null erstellt. Wenn Sie ausstehende Orders ohne Position aufgeben, rufen Sie `ModifyCommand` an, nachdem die Order ausgeführt wurde, um sicherzustellen, dass die neuesten Level angewendet werden.

## Sicherheitsüberlegungen

- Überprüfen Sie immer, ob die Portfolio-, Wertpapier- und Preisschrittinformationen verfügbar sind, bevor Sie eine Bestellung aufgeben. Die Strategie protokolliert Warnungen, wenn erforderliche Daten fehlen.
- Der Parameter `Volume` ist auf die Gerätegrenzen beschränkt. Wenn das angepasste Volumen aufgrund eines inkompatiblen Schritt- oder Mindestvolumens Null wird, wird keine Bestellung gesendet und im Protokoll wird eine Warnung angezeigt.
- Wenn `CloseCommand` ausgeführt wird, storniert die Strategie zunächst Schutzaufträge, dann ausstehende Aufträge und glättet schließlich die Position. Dies spiegelt die defensive Operationsreihenfolge des ursprünglichen Expertenberaters wider.
